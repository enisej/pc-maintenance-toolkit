using Microsoft.Extensions.Configuration;
using PcMaintenanceToolkit.Config;
using PcMaintenanceToolkit.Data;
using PcMaintenanceToolkit.Wizards;
using PcMaintenanceToolkit.Services;

string configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");

if (!File.Exists(configPath))
    CreateConfigWizard.Run(configPath);

var config = new ConfigurationBuilder()
    .AddJsonFile(configPath, optional: false, reloadOnChange: true)
    .Build();

var dbConfig = config.GetSection("Database").Get<DatabaseConfig>()
               ?? throw new InvalidOperationException("Failed to load database config.");

string connectionString = dbConfig.GetConnectionString();
using var db = new AppDbContext(connectionString);
db.Database.EnsureCreated();

Console.WriteLine("=== PC Maintenance Toolkit ===");

var commandService = new CommandService(db);
var logService = new LogService(db);

while (true)
{
    Console.WriteLine("\n1. Run Command");
    Console.WriteLine("2. View Logs");
    Console.WriteLine("3. Manage Commands");
    Console.WriteLine("4. Database Explorer");
    Console.WriteLine("5. SQL Query Runner");
    Console.WriteLine("6. LINQ Queries Demo");
    Console.WriteLine("0. Exit");
    Console.Write("Choose: ");
    var choice = Console.ReadLine();

    switch (choice)
    {
        case "1": commandService.RunCommandMenu(); break;
        case "2": logService.ViewLogs(); break;
        case "3": commandService.ManageCommands(); break;
        case "4": DatabaseExplorer.Run(db); break;
        case "5": SqlQueryRunner.Run(db); break;
        case "6": LinqQueryService.Run(db); break;
        case "0": return;
        default: Console.WriteLine("Invalid."); break;
    }
}