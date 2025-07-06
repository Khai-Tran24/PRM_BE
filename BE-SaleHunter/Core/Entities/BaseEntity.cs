using System.ComponentModel.DataAnnotations;

namespace BE_SaleHunter.Core.Entities
{
    public abstract class BaseEntity
    {
        [Key]
        public long Id { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }
}
