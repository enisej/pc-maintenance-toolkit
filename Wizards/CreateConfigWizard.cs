using System;
using System.IO;
using System.Text.Json;

namespace PcMaintenanceToolkit.Wizards;

public static class CreateConfigWizard
{
    public static void Run(string path)
    {
        Console.WriteLine("=== Database Configuration Wizard ===\n");

        Console.Write("PostgreSQL Host (default: localhost): ");
        string host = Console.ReadLine()!.Trim();
        if (string.IsNullOrWhiteSpace(host)) host = "localhost";

        Console.Write("Port (default: 5432): ");
        string portInput = Console.ReadLine()!.Trim();
        int port = int.TryParse(portInput, out int p) ? p : 5432;

        Console.Write("Database Name (default: pc_maintenance_db): ");
        string dbName = Console.ReadLine()!.Trim();
        if (string.IsNullOrWhiteSpace(dbName)) dbName = "pc_maintenance_db";

        Console.Write("Username (default: postgres): ");
        string user = Console.ReadLine()!.Trim();
        if (string.IsNullOrWhiteSpace(user)) user = "postgres";

        Console.Write("Password: ");
        string password = ReadPassword();

        var config = new
        {
            Database = new
            {
                Host = host,
                Port = port,
                Database = dbName,
                Username = user,
                Password = password
            }
        };

        string json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });

        string? directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllText(path, json);

        Console.WriteLine($"\nConfiguration saved to: {Path.GetFullPath(path)}");
        Console.WriteLine("Press any key to continue...");
        Console.ReadKey();
    }

    private static string ReadPassword()
    {
        var pass = string.Empty;
        ConsoleKey key;
        do
        {
            var keyInfo = Console.ReadKey(intercept: true);
            key = keyInfo.Key;

            if (key == ConsoleKey.Backspace && pass.Length > 0)
            {
                Console.Write("\b \b");
                pass = pass[0..^1];
            }
            else if (!char.IsControl(keyInfo.KeyChar))
            {
                Console.Write("*");
                pass += keyInfo.KeyChar;
            }
        } while (key != ConsoleKey.Enter);

        Console.WriteLine();
        return pass;
    }
}