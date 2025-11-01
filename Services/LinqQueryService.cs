using Microsoft.EntityFrameworkCore;
using PcMaintenanceToolkit.Data;
using PcMaintenanceToolkit.Models;

namespace PcMaintenanceToolkit.Services;

public static class LinqQueryService
{
    public static void Run(AppDbContext db)
    {
        Console.Clear();
        Console.WriteLine("=== LINQ Queries Demo ===\n");

        Query1_WithInput(db);
        Query2_WithInput(db);
        Query3_WithInput(db);

        Query4_NoInput(db);
        Query5_NoInput(db);
        Query6_NoInput(db);

        Console.WriteLine("\nPress any key to go back...");
        Console.ReadKey();
    }

    // 1. Search command by name (case-insensitive)
    static void Query1_WithInput(AppDbContext db)
    {
        Console.Write("Enter command name to search: ");
        string? input = Console.ReadLine();
        if (string.IsNullOrWhiteSpace(input)) return;

        var cmd = db.Commands
            .Where(c => EF.Functions.ILike(c.Name, $"%{input}%"))
            .FirstOrDefault();

        Console.WriteLine(cmd != null
            ? $"Found: {cmd.Id}. {cmd.Name} [{cmd.Type}]"
            : "Not found.");
        Console.WriteLine();
    }

    // 2. Logs in date range
    static void Query2_WithInput(AppDbContext db)
    {
        Console.Write("Enter start date (yyyy-MM-dd): ");
        if (!DateTime.TryParse(Console.ReadLine(), out DateTime start)) return;

        Console.Write("Enter end date (yyyy-MM-dd): ");
        if (!DateTime.TryParse(Console.ReadLine(), out DateTime end)) return;

        var logs = db.Logs
            .Where(l => l.Timestamp >= start && l.Timestamp <= end)
            .OrderBy(l => l.Timestamp)
            .Take(10)
            .ToList();

        Console.WriteLine($"Logs from {start:yyyy-MM-dd} to {end:yyyy-MM-dd}: {logs.Count}");
        foreach (var l in logs)
            Console.WriteLine($"  {l.Timestamp:HH:mm} | {l.Action}");
        Console.WriteLine();
    }

    // 3. Commands by type (case-insensitive)
    static void Query3_WithInput(AppDbContext db)
    {
        Console.Write("Enter command type (SFC/PowerShell/etc): ");
        string? input = Console.ReadLine();
        if (string.IsNullOrWhiteSpace(input)) return;

        var cmds = db.Commands
            .Where(c => EF.Functions.ILike(c.Type, $"%{input}%"))
            .OrderBy(c => c.SortOrder)
            .ToList();

        Console.WriteLine($"Commands matching type '{input}': {cmds.Count}");
        foreach (var c in cmds)
            Console.WriteLine($"  {c.Id}. {c.Name}");
        Console.WriteLine();
    }

    // 4. Top 5 recent logs
    static void Query4_NoInput(AppDbContext db)
    {
        var logs = db.Logs
            .Include(l => l.Category)
            .OrderByDescending(l => l.Timestamp)
            .Take(5)
            .ToList();

        Console.WriteLine("Top 5 recent logs:");
        foreach (var l in logs)
            Console.WriteLine($"  {l.Timestamp:yyyy-MM-dd HH:mm} | {l.Action} | {l.Category?.Name}");
        Console.WriteLine();
    }

    // 5. Command count by type
    static void Query5_NoInput(AppDbContext db)
    {
        var counts = db.Commands
            .GroupBy(c => c.Type)
            .Select(g => new { Type = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .ToList();

        Console.WriteLine("Commands by type:");
        foreach (var x in counts)
            Console.WriteLine($"  {x.Type}: {x.Count}");
        Console.WriteLine();
    }

    // 6. Logs with long output
    static void Query6_NoInput(AppDbContext db)
    {
        var logs = db.Logs
            .Where(l => l.Output != null && l.Output.Length > 100)
            .OrderByDescending(l => l.Output.Length)
            .Take(3)
            .ToList();

        Console.WriteLine("Logs with long output (>100 chars):");
        foreach (var l in logs)
            Console.WriteLine($"  ID {l.Id}: {l.Output!.Length} chars");
        Console.WriteLine();
    }
}