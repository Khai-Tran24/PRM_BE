using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BE_SaleHunter.Core.Entities
{
    public class Store : BaseEntity
    {
        [Required]
        [StringLength(200)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [StringLength(20)]
        public string Type { get; set; } = "local"; // "local" or "online"

        public string? LogoUrl { get; set; }

        [StringLength(20)]
        public string? Phone { get; set; }

        [StringLength(500)]
        public string? Address { get; set; }

        [Required]
        [StringLength(100)]
        public string Category { get; set; } = string.Empty; // niche_market

        [StringLength(1000)]
        public string? Description { get; set; }

        [Column(TypeName = "decimal(10,8)")]
        public decimal? Latitude { get; set; }

        [Column(TypeName = "decimal(11,8)")]
        public decimal? Longitude { get; set; }

        [StringLength(20)]
        public string? WhatsappPhone { get; set; }

        [StringLength(200)]
        public string? FacebookUrl { get; set; }

        [StringLength(200)]
        public string? InstagramUrl { get; set; }

        [StringLength(200)]
        public string? WebsiteUrl { get; set; }

        // Foreign key
        public long UserId { get; set; }

        // Navigation properties
        public virtual User User { get; set; } = null!;
        public virtual ICollection<Product> Products { get; set; } = new List<Product>();
    }
}
