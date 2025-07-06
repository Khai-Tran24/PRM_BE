namespace BE_SaleHunter.Core.Entities
{
    public class UserFavorite : BaseEntity
    {
        // Foreign keys
        public long UserId { get; set; }
        public long ProductId { get; set; }

        // Navigation properties
        public virtual User User { get; set; } = null!;
        public virtual Product Product { get; set; } = null!;
    }
}
