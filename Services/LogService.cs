using PcMaintenanceToolkit.Data;
using PcMaintenanceToolkit.Helpers;
using Microsoft.EntityFrameworkCore;

namespace PcMaintenanceToolkit.Services;

public class LogService
{
    private readonly AppDbContext _db;

    public LogService(AppDbContext db) => _db = db;

    public void ViewLogs()
    {
        var logs = _db.Logs
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

        ViewLogDetail(selectedId);
    }

    private void ViewLogDetail(int logId)
    {
        var log = _db.Logs
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
}