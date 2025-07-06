using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BE_SaleHunter.Core.Entities
{
    public class ProductPrice : BaseEntity
    {
        [Column(TypeName = "decimal(10,2)")]
        public decimal Price { get; set; }

        // Foreign key
        public long ProductId { get; set; }

        // Navigation property
        public virtual Product Product { get; set; } = null!;
    }
}
