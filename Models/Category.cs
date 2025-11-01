namespace PcMaintenanceToolkit.Models
{
    public class Category
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public List<Log> Logs { get; set; } = new();
    }
}
