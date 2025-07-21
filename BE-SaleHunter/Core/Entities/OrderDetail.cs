namespace BE_SaleHunter.Core.Entities
{
    public class OrderDetail : BaseEntity
    {
        public virtual Order? Order { get; set; }
        public virtual Product? Product { get; set; }
        public int? Quantity { get; set; }
        public int? Price { get; set; }
    }
}
