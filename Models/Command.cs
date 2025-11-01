using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PcMaintenanceToolkit.Models
{
    [Table("Commands")]
    public class Command
    {
        [Key]
        public int Id { get; set; }

        [Required, StringLength(100)]
        public string Name { get; set; } = null!;

        [Required, StringLength(50)]
        public string Type { get; set; } = null!;

        [Required]
        public string Script { get; set; } = null!;

        [StringLength(500)]
        public string? Description { get; set; }

        [Range(0, 999)]
        public int SortOrder { get; set; } = 99;

        public DateTime CreatedAt { get; set; }
    }
}
