using Microsoft.EntityFrameworkCore;
using PcMaintenanceToolkit.Data;
using System.Reflection;

namespace PcMaintenanceToolkit.Services
{
    public static class DatabaseExplorer
    {
        private static readonly Dictionary<Type, string> TableMap = new()
        {
            { typeof(PcMaintenanceToolkit.Models.Category), "Categories" },
            { typeof(PcMaintenanceToolkit.Models.Command), "Commands" },
            { typeof(PcMaintenanceToolkit.Models.Log), "Logs" }
        };

        public static void Run(AppDbContext db)
        {
            while (true)
            {
                Console.Clear();
                Console.WriteLine("=== Database Explorer ===\n");
                Console.WriteLine("Available tables:");

                var tableEntries = GetDbSetEntries(db);
                for (int i = 0; i < tableEntries.Count; i++)
                    Console.WriteLine($"  {i + 1}. {tableEntries[i].DisplayName}");

                Console.WriteLine("  0. Back");
                Console.Write("\nChoose table: ");
                if (!int.TryParse(Console.ReadLine(), out int choice) || choice == 0) return;

                if (choice < 1 || choice > tableEntries.Count)
                {
                    Console.WriteLine("Invalid choice.");
                    Console.ReadKey();
                    continue;
                }

                var selected = tableEntries[choice - 1];
                BrowseTable(db, selected.SetProperty, selected.EntityType, selected.DisplayName);
            }
        }

        static List<(PropertyInfo SetProperty, Type EntityType, string DisplayName)> GetDbSetEntries(AppDbContext db)
        {
            var result = new List<(PropertyInfo, Type, string)>();
            var props = typeof(AppDbContext).GetProperties();

            foreach (var prop in props)
            {
                if (prop.PropertyType.IsGenericType &&
                    prop.PropertyType.GetGenericTypeDefinition() == typeof(DbSet<>))
                {
                    var entityType = prop.PropertyType.GetGenericArguments()[0];
                    if (TableMap.TryGetValue(entityType, out string? name))
                    {
                        result.Add((prop, entityType, name));
                    }
                }
            }
            return result;
        }

        static void BrowseTable(AppDbContext db, PropertyInfo setProperty, Type entityType, string displayName)
        {
            var queryable = (IQueryable<object>)setProperty.GetValue(db)!;
            var items = queryable.ToList();

            while (true)
            {
                Console.Clear();
                Console.WriteLine($"=== {displayName} ({items.Count} rows) ===\n");
                PrintTable(items, entityType);

                Console.WriteLine("\n1. View Row");
                Console.WriteLine("2. Edit Row");
                Console.WriteLine("3. Delete Row");
                Console.WriteLine("0. Back");
                Console.Write("Choose: ");
                var action = Console.ReadLine();

                switch (action)
                {
                    case "1": ViewRow(items, entityType); break;
                    case "2": EditRow(db, items, entityType); break;
                    case "3": DeleteRow(db, items, entityType); break;
                    case "0": return;
                    default: Console.WriteLine("Invalid."); break;
                }
                Console.ReadKey();
            }
        }

        static void PrintTable(List<object> items, Type entityType)
        {
            if (!items.Any())
            {
                Console.WriteLine("No data.");
                return;
            }

            var props = entityType.GetProperties();
            foreach (var p in props.Take(5))
                Console.Write($"{p.Name,-20}");
            Console.WriteLine();

            foreach (var item in items.Take(20))
            {
                foreach (var p in props.Take(5))
                {
                    var val = p.GetValue(item);
                    Console.Write($"{(val?.ToString() ?? "NULL"),-20}");
                }
                Console.WriteLine();
            }
            if (items.Count > 20) Console.WriteLine($"... and {items.Count - 20} more");
        }

        static object? FindById(List<object> items, int id, Type entityType)
        {
            var idProp = entityType.GetProperty("Id");
            if (idProp == null) return null;

            foreach (var item in items)
            {
                var value = idProp.GetValue(item);
                if (value is int intId && intId == id)
                    return item;
            }
            return null;
        }

        static void ViewRow(List<object> items, Type entityType)
        {
            Console.Write("Enter ID: ");
            if (!int.TryParse(Console.ReadLine(), out int id)) return;
            var item = FindById(items, id, entityType);
            if (item == null)
            {
                Console.WriteLine("Not found.");
                return;
            }

            var props = entityType.GetProperties();
            foreach (var p in props)
                Console.WriteLine($"{p.Name}: {p.GetValue(item)}");
        }

        static void EditRow(AppDbContext db, List<object> items, Type entityType)
        {
            Console.Write("Enter ID to edit: ");
            if (!int.TryParse(Console.ReadLine(), out int id)) return;
            var item = FindById(items, id, entityType);
            if (item == null)
            {
                Console.WriteLine("Not found.");
                return;
            }

            var props = entityType.GetProperties();
            foreach (var p in props)
            {
                if (p.Name.Equals("Id", StringComparison.OrdinalIgnoreCase)) continue;

                Console.Write($"{p.Name} [{p.GetValue(item)}]: ");
                string? input = Console.ReadLine();
                if (!string.IsNullOrWhiteSpace(input))
                {
                    try
                    {
                        var converted = Convert.ChangeType(input, p.PropertyType);
                        p.SetValue(item, converted);
                    }
                    catch
                    {
                        Console.WriteLine($"Invalid value for {p.Name}");
                    }
                }
            }
            db.SaveChanges();
            Console.WriteLine("Updated!");
        }

        static void DeleteRow(AppDbContext db, List<object> items, Type entityType)
        {
            Console.Write("Enter ID to delete: ");
            if (!int.TryParse(Console.ReadLine(), out int id)) return;
            var item = FindById(items, id, entityType);
            if (item == null)
            {
                Console.WriteLine("Not found.");
                return;
            }

            Console.Write("Delete? (y/N): ");
            if (Console.ReadLine()?.Trim().ToLower() == "y")
            {
                var entry = db.Entry(item);
                entry.State = EntityState.Deleted;
                db.SaveChanges();
                Console.WriteLine("Deleted.");
            }
        }
    }
}