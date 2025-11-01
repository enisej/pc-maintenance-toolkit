namespace PcMaintenanceToolkit.Models
{
    public class Log
    {
        public int Id { get; set; }
        public string Action { get; set; } = "";
        public int CategoryId { get; set; }
        public Category? Category { get; set; }
        public DateTime Timestamp { get; set; }  // ← Auto-UTC
        public string? Output { get; set; }
    }
}
