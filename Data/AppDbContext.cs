using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using PcMaintenanceToolkit.Models;

namespace PcMaintenanceToolkit.Data;

public class AppDbContext : DbContext
{
    private readonly string _connectionString;

    public AppDbContext(string connectionString)
    {
        _connectionString = connectionString;
    }

    public DbSet<Command> Commands => Set<Command>();
    public DbSet<Log> Logs => Set<Log>();
    public DbSet<Category> Categories => Set<Category>();

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseNpgsql(_connectionString);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // ————————————————————
        // 1. UTC Converter (auto-convert all DateTime to UTC)
        // ————————————————————
        var utcConverter = new ValueConverter<DateTime, DateTime>(
            v => v.ToUniversalTime(),
            v => DateTime.SpecifyKind(v, DateTimeKind.Utc));

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            foreach (var property in entityType.GetProperties())
            {
                if (property.ClrType == typeof(DateTime) || property.ClrType == typeof(DateTime?))
                {
                    property.SetValueConverter(utcConverter);
                }
            }
        }

        // ————————————————————
        // 2. Seed Categories
        // ————————————————————
        modelBuilder.Entity<Category>().HasData(
            new Category { Id = 1, Name = "System" },
            new Category { Id = 2, Name = "Power" },
            new Category { Id = 3, Name = "Disk" },
            new Category { Id = 4, Name = "Custom" }
        );

        // ————————————————————
        // 3. Seed Built-in Commands — USE FIXED TIMESTAMP!
        // ————————————————————
        var fixedSeedTime = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        modelBuilder.Entity<Command>().HasData(
            new Command
            {
                Id = 1,
                Name = "Run SFC Scan",
                Script = "sfc /scannow",
                Type = "SFC",
                Description = "Scans and repairs system files",
                SortOrder = 10,
                CreatedAt = fixedSeedTime  // ← STATIC!
            },
            new Command
            {
                Id = 2,
                Name = "Generate Power Report",
                Script = "powercfg /batteryreport /output \"battery-report.html\"",
                Type = "PowerReport",
                Description = "Creates battery usage report",
                SortOrder = 20,
                CreatedAt = fixedSeedTime
            },
            new Command
            {
                Id = 3,
                Name = "Check Disk Usage",
                Script = "diskusage",
                Type = "DiskCheck",
                Description = "Shows disk space usage",
                SortOrder = 30,
                CreatedAt = fixedSeedTime
            }
        );

        // ————————————————————
        // 4. Indexes
        // ————————————————————
        modelBuilder.Entity<Log>()
            .HasIndex(l => l.Timestamp);

        modelBuilder.Entity<Command>()
            .HasIndex(c => c.SortOrder);
    }
}