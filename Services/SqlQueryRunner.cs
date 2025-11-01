using Microsoft.EntityFrameworkCore;
using PcMaintenanceToolkit.Data;

namespace PcMaintenanceToolkit.Services;

public static class SqlQueryRunner
{
    public static void Run(AppDbContext db)
    {
        Console.Clear();
        Console.WriteLine("=== SQL Query Runner ===\n");
        Console.WriteLine("Enter SQL (SELECT, INSERT, UPDATE, DELETE)");
        Console.WriteLine("Use double quotes for table names: \"Commands\", \"Logs\"");
        Console.WriteLine("Type 'EXIT' to go back\n");

        while (true)
        {
            Console.Write("SQL> ");
            string? sql = Console.ReadLine();
            if (sql?.Trim().ToUpper() == "EXIT") return;
            if (string.IsNullOrWhiteSpace(sql)) continue;

            try
            {
                if (sql.Trim().StartsWith("SELECT", StringComparison.OrdinalIgnoreCase))
                {
                    var results = ExecuteSelect(db, sql);
                    PrintResults(results);
                }
                else
                {
                    int affected = db.Database.ExecuteSqlRaw(sql);
                    Console.WriteLine($"Affected rows: {affected}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR: {ex.Message}");
            }
            Console.WriteLine();
        }
    }

    static List<Dictionary<string, object?>> ExecuteSelect(AppDbContext db, string sql)
    {
        var results = new List<Dictionary<string, object?>>();
        using var command = db.Database.GetDbConnection().CreateCommand();
        command.CommandText = sql;

        if (command.Connection?.State != System.Data.ConnectionState.Open)
            command.Connection?.Open();

        using var reader = command.ExecuteReader();
        var columnNames = Enumerable.Range(0, reader.FieldCount)
            .Select(i => reader.GetName(i))
            .ToList();

        while (reader.Read())
        {
            var row = new Dictionary<string, object?>();
            foreach (var col in columnNames)
            {
                row[col] = reader[col] is DBNull ? null : reader[col];
            }
            results.Add(row);
        }

        return results;
    }

    static void PrintResults(List<Dictionary<string, object?>> results)
    {
        if (!results.Any())
        {
            Console.WriteLine("No results.");
            return;
        }

        var firstRow = results[0];
        foreach (var key in firstRow.Keys)
            Console.Write($"{key,-20}");
        Console.WriteLine();

        foreach (var row in results.Take(50))
        {
            foreach (var key in firstRow.Keys)
            {
                var val = row[key];
                Console.Write($"{(val?.ToString() ?? "NULL"),-20}");
            }
            Console.WriteLine();
        }

        if (results.Count > 50)
            Console.WriteLine($"... and {results.Count - 50} more");
    }
}