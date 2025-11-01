namespace PcMaintenanceToolkit.Models
{
    public class Command
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string Script { get; set; } = "";
        public string Type { get; set; } = "";
        public string? Description { get; set; }
        public int SortOrder { get; set; } = 0;
        public DateTime CreatedAt { get; set; }
    }
}
