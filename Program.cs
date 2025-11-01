// Program.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using PcMaintenanceToolkit.Config;
using PcMaintenanceToolkit.Data;
using PcMaintenanceToolkit.Helpers;   // ← NEW
using PcMaintenanceToolkit.Models;
using System.Diagnostics;
using System.IO;

var config = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddEnvironmentVariables()
    .Build();

var dbConfig = config.GetSection("Database").Get<DatabaseConfig>();
string connectionString = dbConfig.GetConnectionString();

using var db = new AppDbContext(connectionString);
db.Database.EnsureCreated();

Console.WriteLine("=== PC Maintenance Toolkit ===");

while (true)
{
    Console.WriteLine("\n1. Run Command");
    Console.WriteLine("2. View Logs");
    Console.WriteLine("3. Manage Commands (Add/Edit/Delete)");
    Console.WriteLine("0. Exit");
    Console.Write("Choose: ");
    var choice = Console.ReadLine();

    switch (choice)
    {
        case "1": RunCommandMenu(db); break;
        case "2": ViewLogs(db); break;
        case "3": ManageCommands(db); break;
        case "0": return;
        default: Console.WriteLine("Invalid."); break;
    }
}

// ———————————————————————— HELPERS ————————————————————————

void ViewLogs(AppDbContext db)
{
    var logs = db.Logs
        .Include(l => l.Category)
        .OrderByDescending(l => l.Timestamp)
        .Take(20)
        .ToList();

    if (!logs.Any())
    {
        Console.WriteLine("No logs yet.");
        return;
    }

    Console.WriteLine("\n-- Recent Logs (choose ID to view details) --");
    foreach (var log in logs)
    {
        string category = log.Category?.Name ?? "Unknown";
        Console.WriteLine($"{log.Id}: {log.Timestamp.ToLocalTime():yyyy-MM-dd HH:mm} | {log.Action} | {category}");
    }

    Console.Write("\nEnter Log ID to view output (or 0 to go back): ");
    if (!int.TryParse(Console.ReadLine(), out int selectedId) || selectedId == 0)
        return;

    ViewLogDetail(db, selectedId);
}

void ViewLogDetail(AppDbContext db, int logId)
{
    var log = db.Logs
        .Include(l => l.Category)
        .FirstOrDefault(l => l.Id == logId);

    if (log == null)
    {
        Console.WriteLine("Log not found.");
        return;
    }

    string category = log.Category?.Name ?? "Unknown";
    Console.WriteLine($"\n=== LOG #{log.Id} ===");
    Console.WriteLine($"Time: {log.Timestamp.ToLocalTime():yyyy-MM-dd HH:mm:ss}");
    Console.WriteLine($"Action: {log.Action}");
    Console.WriteLine($"Category: {category}");
    Console.WriteLine(new string('=', 50));

    if (string.IsNullOrWhiteSpace(log.Output))
    {
        Console.WriteLine("No output recorded.");
    }
    else
    {
        Console.WriteLine("OUTPUT:");
        Console.WriteLine(ToolkitHelpers.RemoveNullChars(log.Output));
    }

    Console.WriteLine(new string('=', 50));
    Console.WriteLine("Press any key to go back...");
    Console.ReadKey();
}

void RunCommandMenu(AppDbContext db)
{
    var commands = db.Commands
        .OrderBy(c => c.SortOrder)
        .ToList();

    if (!commands.Any())
    {
        Console.WriteLine("No commands in database.");
        return;
    }

    Console.WriteLine("\n-- Available Commands --");
    foreach (var c in commands)
        Console.WriteLine($"{c.Id}. {c.Name} [{c.Type}]");

    Console.Write("Enter ID to run: ");
    if (!int.TryParse(Console.ReadLine(), out int id)) return;

    var cmd = commands.FirstOrDefault(c => c.Id == id);
    if (cmd == null)
    {
        Console.WriteLine("Command not found.");
        return;
    }

    ExecuteCommand(cmd, db);
}

void ExecuteCommand(Command cmd, AppDbContext db)
{
    Console.WriteLine($"Running: {cmd.Name}");

    string stdout = "";
    string stderr = "";
    bool success = true;

    try
    {
        switch (cmd.Type)
        {
            case "SFC":
                RunProcess("cmd.exe", $"/c {cmd.Script}", out stdout, out stderr);
                break;

            case "PowerReport":
                RunProcess("powercfg", cmd.Script, out stdout, out stderr);
                break;

            case "DiskCheck":
                stdout = GetDiskUsage();
                break;

            case "PowerShell":
                RunProcess("powershell.exe", $"-NoProfile -Command \"{cmd.Script}\"", out stdout, out stderr);
                break;

            default:
                stdout = "Unknown command type.";
                success = false;
                break;
        }
    }
    catch (Exception ex)
    {
        stderr = ex.Message;
        success = false;
    }

    // ———— SHOW OUTPUT IN CONSOLE ————
    if (!string.IsNullOrWhiteSpace(stdout))
        Console.WriteLine("\n[OUTPUT]\n" + ToolkitHelpers.RemoveNullChars(stdout));

    if (!string.IsNullOrWhiteSpace(stderr))
        Console.WriteLine("\n[ERROR]\n" + ToolkitHelpers.RemoveNullChars(stderr));

    // ———— SAVE TO DATABASE ————
    string result = success
        ? ToolkitHelpers.RemoveNullChars(stdout)
        : $"ERROR: {ToolkitHelpers.RemoveNullChars(stderr)}\nOUTPUT: {ToolkitHelpers.RemoveNullChars(stdout)}";

    db.Logs.Add(new Log
    {
        Action = cmd.Name,
        CategoryId = cmd.Type switch
        {
            "SFC" => 1,
            "PowerReport" => 2,
            "DiskCheck" => 3,
            _ => 4
        },
        Timestamp = ToolkitHelpers.UtcNow,
        Output = result.Length > 3000 ? result[..3000] + "…" : result  // increased limit
    });
    db.SaveChanges();

    Console.WriteLine(success ? "\nDone. Output saved to logs." : "\nFailed. See logs.");
}

void RunProcess(string fileName, string arguments, out string stdout, out string stderr)
{
    var psi = new ProcessStartInfo
    {
        FileName = fileName,
        Arguments = arguments,
        UseShellExecute = false,
        RedirectStandardOutput = true,
        RedirectStandardError = true,
        CreateNoWindow = true,
        StandardOutputEncoding = System.Text.Encoding.UTF8,
        StandardErrorEncoding = System.Text.Encoding.UTF8
    };

    using var process = new Process { StartInfo = psi };
    process.Start();

    // Read output ASYNC to prevent deadlock
    var outputTask = process.StandardOutput.ReadToEndAsync();
    var errorTask = process.StandardError.ReadToEndAsync();

    process.WaitForExit();

    stdout = outputTask.Result;
    stderr = errorTask.Result;
}

string GetDiskUsage()
{
    var drives = DriveInfo.GetDrives()
        .Where(d => d.IsReady && d.DriveType == DriveType.Fixed);

    var sb = new System.Text.StringBuilder();
    foreach (var d in drives)
    {
        long used = d.TotalSize - d.AvailableFreeSpace;
        double pct = (double)used / d.TotalSize * 100;
        sb.AppendLine($"{d.Name} {pct:F1}% used ({d.AvailableFreeSpace / 1e9:F1} GB free)");
    }
    return sb.ToString();
}

// ———————————————————————— COMMAND MANAGEMENT ————————————————————————

void ManageCommands(AppDbContext db)
{
    while (true)
    {
        Console.WriteLine("\n--- Manage Commands ---");
        Console.WriteLine("1. List All");
        Console.WriteLine("2. Add New");
        Console.WriteLine("3. Edit");
        Console.WriteLine("4. Delete");
        Console.WriteLine("0. Back");
        Console.Write("Choose: ");
        var choice = Console.ReadLine();

        switch (choice)
        {
            case "1": ListAllCommands(db); break;
            case "2": AddCommand(db); break;
            case "3": EditCommand(db); break;
            case "4": DeleteCommand(db); break;
            case "0": return;
            default: Console.WriteLine("Invalid."); break;
        }
    }
}

void ListAllCommands(AppDbContext db)
{
    var cmds = db.Commands.OrderBy(c => c.SortOrder).ToList();
    Console.WriteLine("\n-- All Commands --");
    foreach (var c in cmds)
        Console.WriteLine($"{c.Id}. {c.Name} [{c.Type}]\n    → {c.Script}\n    {c.Description ?? "No description"}\n");
}

void AddCommand(AppDbContext db)
{
    Console.Write("Name: "); string name = Console.ReadLine() ?? "";
    Console.Write("Type (SFC/PowerReport/DiskCheck/PowerShell): "); string type = Console.ReadLine() ?? "";
    Console.Write("Script: "); string script = Console.ReadLine() ?? "";
    Console.Write("Description (optional): "); string desc = Console.ReadLine() ?? "";
    Console.Write("Sort Order (number): "); int order = int.TryParse(Console.ReadLine(), out int o) ? o : 99;

    db.Commands.Add(new Command
    {
        Name = name,
        Type = type,
        Script = script,
        Description = string.IsNullOrWhiteSpace(desc) ? null : desc,
        SortOrder = order,
        CreatedAt = ToolkitHelpers.UtcNow
    });
    db.SaveChanges();
    Console.WriteLine("Command added!");
}

void EditCommand(AppDbContext db)
{
    ListAllCommands(db);
    Console.Write("Enter ID to edit: ");
    if (!int.TryParse(Console.ReadLine(), out int id)) return;

    var cmd = db.Commands.Find(id);
    if (cmd == null) { Console.WriteLine("Not found."); return; }

    Console.WriteLine($"Editing: {cmd.Name}");
    Console.Write($"Name [{cmd.Name}]: "); string? n = Console.ReadLine(); if (!string.IsNullOrWhiteSpace(n)) cmd.Name = n;
    Console.Write($"Type [{cmd.Type}]: "); string? t = Console.ReadLine(); if (!string.IsNullOrWhiteSpace(t)) cmd.Type = t;
    Console.Write($"Script [{cmd.Script}]: "); string? s = Console.ReadLine(); if (!string.IsNullOrWhiteSpace(s)) cmd.Script = s;
    Console.Write($"Description [{cmd.Description ?? "none"}]: "); string? d = Console.ReadLine();
    if (!string.IsNullOrWhiteSpace(d)) cmd.Description = d;
    Console.Write($"Sort Order [{cmd.SortOrder}]: "); if (int.TryParse(Console.ReadLine(), out int o)) cmd.SortOrder = o;

    cmd.CreatedAt = ToolkitHelpers.UtcNow;   // optional: update timestamp
    db.SaveChanges();
    Console.WriteLine("Updated!");
}

void DeleteCommand(AppDbContext db)
{
    ListAllCommands(db);
    Console.Write("Enter ID to delete: ");
    if (!int.TryParse(Console.ReadLine(), out int id)) return;

    var cmd = db.Commands.Find(id);
    if (cmd == null) { Console.WriteLine("Not found."); return; }

    Console.Write($"Delete '{cmd.Name}'? (y/N): ");
    if (Console.ReadLine()?.Trim().ToLower() == "y")
    {
        db.Commands.Remove(cmd);
        db.SaveChanges();
        Console.WriteLine("Deleted.");
    }
}