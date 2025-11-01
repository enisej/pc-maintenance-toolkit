namespace PcMaintenanceToolkit.Config
{
    public class DatabaseConfig
    {
        public string Host { get; set; } = "localhost";
        public int Port { get; set; } = 5432;
        public string Database { get; set; } = "pc_maintenance_db";
        public string Username { get; set; } = "postgres";
        public string Password { get; set; } = "";

        public string GetConnectionString()
        {
            return $"Host={Host};Port={Port};Database={Database};Username={Username};Password={Password};";
        }
    }
}
