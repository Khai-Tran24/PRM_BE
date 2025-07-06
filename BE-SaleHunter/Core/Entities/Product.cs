using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BE_SaleHunter.Core.Entities
{
    public class Product : BaseEntity
    {
        [Required]
        [StringLength(200)]
        public string Name { get; set; } = string.Empty;

        [StringLength(200)]
        public string? NameArabic { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal Price { get; set; }

        public int SalePercent { get; set; } = 0;

        [StringLength(100)]
        public string? Brand { get; set; }

        [Required]
        [StringLength(100)]
        public string Category { get; set; } = string.Empty;

        [StringLength(100)]
        public string? CategoryArabic { get; set; }

        [StringLength(2000)]
        public string? Description { get; set; }

        [StringLength(2000)]
        public string? DescriptionArabic { get; set; }

        [StringLength(500)]
        public string? SourceUrl { get; set; }
        
        // Foreign key
        public long StoreId { get; set; }

        // Navigation properties
        public virtual Store Store { get; set; } = null!;
        public virtual ICollection<ProductImage> Images { get; set; } = new List<ProductImage>();
        public virtual ICollection<ProductPrice> PriceHistory { get; set; } = new List<ProductPrice>();
        public virtual ICollection<ProductRating> Ratings { get; set; } = new List<ProductRating>();
        public virtual ICollection<UserFavorite> Favorites { get; set; } = new List<UserFavorite>();
        public virtual ICollection<ProductView> Views { get; set; } = new List<ProductView>();

        // Calculated properties
        public decimal FinalPrice => Price - (Price * SalePercent / 100);
        public double AverageRating => Ratings.Any() ? Ratings.Average(r => r.Rating) : 0.0;
        public int TotalRatings => Ratings.Count;
        public int TotalViews => Views.Count;
    }
}
