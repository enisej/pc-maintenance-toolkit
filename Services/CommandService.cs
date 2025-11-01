using PcMaintenanceToolkit.Data;
using PcMaintenanceToolkit.Models;
using PcMaintenanceToolkit.Helpers;
using System.Diagnostics;

namespace PcMaintenanceToolkit.Services;

public class CommandService
{
    private readonly AppDbContext _db;

    public CommandService(AppDbContext db) => _db = db;

    public void RunCommandMenu()
    {
        var commands = _db.Commands.OrderBy(c => c.SortOrder).ToList();
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

        ExecuteCommand(cmd);
    }

    public void ExecuteCommand(Command cmd)
    {
        Console.WriteLine($"Running: {cmd.Name}");
        string stdout = "", stderr = "";
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

        if (!string.IsNullOrWhiteSpace(stdout))
            Console.WriteLine("\n[OUTPUT]\n" + ToolkitHelpers.RemoveNullChars(stdout));
        if (!string.IsNullOrWhiteSpace(stderr))
            Console.WriteLine("\n[ERROR]\n" + ToolkitHelpers.RemoveNullChars(stderr));

        string result = success
            ? ToolkitHelpers.RemoveNullChars(stdout)
            : $"ERROR: {ToolkitHelpers.RemoveNullChars(stderr)}\nOUTPUT: {ToolkitHelpers.RemoveNullChars(stdout)}";

        _db.Logs.Add(new Log
        {
            Action = cmd.Name,
            CategoryId = cmd.Type switch { "SFC" => 1, "PowerReport" => 2, "DiskCheck" => 3, _ => 4 },
            Timestamp = ToolkitHelpers.UtcNow,
            Output = result.Length > 3000 ? result[..3000] + "…" : result
        });
        _db.SaveChanges();

        Console.WriteLine(success ? "\nDone. Output saved to logs." : "\nFailed. See logs.");
    }

    private void RunProcess(string fileName, string arguments, out string stdout, out string stderr)
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

        var outputTask = process.StandardOutput.ReadToEndAsync();
        var errorTask = process.StandardError.ReadToEndAsync();
        process.WaitForExit();

        stdout = outputTask.Result;
        stderr = errorTask.Result;
    }

    private string GetDiskUsage()
    {
        var drives = DriveInfo.GetDrives().Where(d => d.IsReady && d.DriveType == DriveType.Fixed);
        var sb = new System.Text.StringBuilder();
        foreach (var d in drives)
        {
            long used = d.TotalSize - d.AvailableFreeSpace;
            double pct = (double)used / d.TotalSize * 100;
            sb.AppendLine($"{d.Name} {pct:F1}% used ({d.AvailableFreeSpace / 1e9:F1} GB free)");
        }
        return sb.ToString();
    }

    public void ManageCommands()
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
                case "1": ListAllCommands(); break;
                case "2": AddCommand(); break;
                case "3": EditCommand(); break;
                case "4": DeleteCommand(); break;
                case "0": return;
                default: Console.WriteLine("Invalid."); break;
            }
        }
    }

    private void ListAllCommands()
    {
        var cmds = _db.Commands.OrderBy(c => c.SortOrder).ToList();
        Console.WriteLine("\n-- All Commands --");
        foreach (var c in cmds)
            Console.WriteLine($"{c.Id}. {c.Name} [{c.Type}]\n    → {c.Script}\n    {c.Description ?? "No description"}\n");
    }

    private void AddCommand()
    {
        Console.Write("Name: "); string name = Console.ReadLine() ?? "";
        Console.Write("Type (SFC/PowerReport/DiskCheck/PowerShell): "); string type = Console.ReadLine() ?? "";
        Console.Write("Script: "); string script = Console.ReadLine() ?? "";
        Console.Write("Description (optional): "); string desc = Console.ReadLine() ?? "";
        Console.Write("Sort Order (number): "); int order = int.TryParse(Console.ReadLine(), out int o) ? o : 99;

        _db.Commands.Add(new Command
        {
            Name = name,
            Type = type,
            Script = script,
            Description = string.IsNullOrWhiteSpace(desc) ? null : desc,
            SortOrder = order,
            CreatedAt = ToolkitHelpers.UtcNow
        });
        _db.SaveChanges();
        Console.WriteLine("Command added!");
    }

    private void EditCommand()
    {
        ListAllCommands();
        Console.Write("Enter ID to edit: ");
        if (!int.TryParse(Console.ReadLine(), out int id)) return;

        var cmd = _db.Commands.Find(id);
        if (cmd == null) { Console.WriteLine("Not found."); return; }

        Console.WriteLine($"Editing: {cmd.Name}");
        Console.Write($"Name [{cmd.Name}]: "); string? n = Console.ReadLine(); if (!string.IsNullOrWhiteSpace(n)) cmd.Name = n;
        Console.Write($"Type [{cmd.Type}]: "); string? t = Console.ReadLine(); if (!string.IsNullOrWhiteSpace(t)) cmd.Type = t;
        Console.Write($"Script [{cmd.Script}]: "); string? s = Console.ReadLine(); if (!string.IsNullOrWhiteSpace(s)) cmd.Script = s;
        Console.Write($"Description [{cmd.Description ?? "none"}]: "); string? d = Console.ReadLine();
        if (!string.IsNullOrWhiteSpace(d)) cmd.Description = d;
        Console.Write($"Sort Order [{cmd.SortOrder}]: "); if (int.TryParse(Console.ReadLine(), out int o)) cmd.SortOrder = o;

        cmd.CreatedAt = ToolkitHelpers.UtcNow;
        _db.SaveChanges();
        Console.WriteLine("Updated!");
    }

    private void DeleteCommand()
    {
        ListAllCommands();
        Console.Write("Enter ID to delete: ");
        if (!int.TryParse(Console.ReadLine(), out int id)) return;

        var cmd = _db.Commands.Find(id);
        if (cmd == null) { Console.WriteLine("Not found."); return; }

        Console.Write($"Delete '{cmd.Name}'? (y/N): ");
        if (Console.ReadLine()?.Trim().ToLower() == "y")
        {
            _db.Commands.Remove(cmd);
            _db.SaveChanges();
            Console.WriteLine("Deleted.");
        }
    }
}