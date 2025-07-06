namespace BE_SaleHunter.Core.Entities
{
    public class ProductView : BaseEntity
    {
        // Foreign keys
        public long UserId { get; set; }
        public long ProductId { get; set; }

        public DateTime ViewedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual User User { get; set; } = null!;
        public virtual Product Product { get; set; } = null!;
    }
}
