using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using PcMaintenanceToolkit.Models;

namespace PcMaintenanceToolkit.Data
{
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
            // === UTC Converter for all DateTime ===
            var utcConverter = new ValueConverter<DateTime, DateTime>(
                v => v.ToUniversalTime(),
                v => DateTime.SpecifyKind(v, DateTimeKind.Utc));

            var nullableUtcConverter = new ValueConverter<DateTime?, DateTime?>(
                v => v.HasValue ? v.Value.ToUniversalTime() : v,
                v => v.HasValue ? DateTime.SpecifyKind(v.Value, DateTimeKind.Utc) : v);

            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                foreach (var property in entityType.GetProperties())
                {
                    if (property.ClrType == typeof(DateTime))
                        property.SetValueConverter(utcConverter);
                    else if (property.ClrType == typeof(DateTime?))
                        property.SetValueConverter(nullableUtcConverter);
                }
            }

            // === TABLE NAMES (Exact) ===
            modelBuilder.Entity<Command>().ToTable("Commands");
            modelBuilder.Entity<Log>().ToTable("Logs");
            modelBuilder.Entity<Category>().ToTable("Categories");

            // === COMMAND CONFIG ===
            modelBuilder.Entity<Command>(entity =>
            {
                entity.HasKey(c => c.Id);
                entity.Property(c => c.Name).IsRequired().HasMaxLength(100);
                entity.Property(c => c.Type).IsRequired().HasMaxLength(50);
                entity.Property(c => c.Script).IsRequired();
                entity.Property(c => c.Description).HasMaxLength(500);
                entity.Property(c => c.SortOrder).HasDefaultValue(99);

                entity.HasIndex(c => c.Name).IsUnique();
                entity.HasIndex(c => c.SortOrder);

                entity.Property(c => c.CreatedAt)
                      .HasDefaultValueSql("NOW()")
                      .ValueGeneratedOnAdd();
            });

            // === CATEGORY CONFIG + SEED ===
            modelBuilder.Entity<Category>(entity =>
            {
                entity.HasKey(c => c.Id);
                entity.Property(c => c.Name).IsRequired().HasMaxLength(50);

                entity.HasData(
                    new Category { Id = 1, Name = "SFC" },
                    new Category { Id = 2, Name = "PowerReport" },
                    new Category { Id = 3, Name = "DiskCheck" },
                    new Category { Id = 4, Name = "PowerShell" }
                );
            });

            // === LOG CONFIG + RELATIONSHIP + CASCADE DELETE ===
            modelBuilder.Entity<Log>(entity =>
            {
                entity.HasKey(l => l.Id);
                entity.Property(l => l.Action).IsRequired().HasMaxLength(200);
                entity.Property(l => l.Output).HasMaxLength(4000);

                entity.HasIndex(l => l.Timestamp);

                // Foreign Key + Cascade Delete
                entity.HasOne(l => l.Category)
                      .WithMany(c => c.Logs)
                      .HasForeignKey(l => l.CategoryId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // === SEED COMMANDS ===
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
                    CreatedAt = fixedSeedTime
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
        }
    }
}