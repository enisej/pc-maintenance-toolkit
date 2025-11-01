using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PcMaintenanceToolkit.Models
{
    [Table("Logs")]
    public class Log
    {
        [Key]
        public int Id { get; set; }

        [Required, StringLength(200)]
        public string Action { get; set; } = null!;

        public int CategoryId { get; set; }

        public DateTime Timestamp { get; set; }

        [StringLength(4000)]
        public string? Output { get; set; }

        [ForeignKey("CategoryId")]
        public Category? Category { get; set; }
    }
}
