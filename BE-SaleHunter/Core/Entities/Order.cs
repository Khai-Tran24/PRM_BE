using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BE_SaleHunter.Core.Entities
{
    public class Order : BaseEntity
    {
        public DateTime? OrderDate { get; set; }
        public DateTime? ShipDate { get; set; }
        public string? PaymentMethod { get; set; }
        public string? Status { get; set; }
        public string? ShipName { get; set; }
        public int? TotalPrice { get; set; }


        [ForeignKey("CustomerId")]
        public virtual User? User { get; set; }

        public virtual ICollection<OrderDetail>? OrderDetails { get; set; }
    }
}
