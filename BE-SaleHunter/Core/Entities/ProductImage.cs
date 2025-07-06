using System.ComponentModel.DataAnnotations;

namespace BE_SaleHunter.Core.Entities
{
    public class ProductImage : BaseEntity
    {
        [Required]
        [StringLength(500)]
        public string ImageUrl { get; set; } = string.Empty;

        public int DisplayOrder { get; set; } = 0;

        // Foreign key
        public long ProductId { get; set; }

        // Navigation property
        public virtual Product Product { get; set; } = null!;
    }
}
