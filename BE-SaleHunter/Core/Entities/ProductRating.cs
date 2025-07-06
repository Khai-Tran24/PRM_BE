using System.ComponentModel.DataAnnotations;

namespace BE_SaleHunter.Core.Entities
{
    public class ProductRating : BaseEntity
    {
        [Range(1, 5)]
        public int Rating { get; set; }

        [StringLength(500)]
        public string? Comment { get; set; }

        // Foreign keys
        public long ProductId { get; set; }
        public long UserId { get; set; }

        // Navigation properties
        public virtual Product Product { get; set; } = null!;
        public virtual User User { get; set; } = null!;
    }
}
