using BE_SaleHunter.Core.Entities;

namespace BE_SaleHunter.Application.DTOs
{
    public class OrderDto
    {
        public DateTime? OrderDate { get; set; }
        public DateTime? ShipDate { get; set; }
        public string? PaymentMethod { get; set; }
        public string? Status { get; set; }
        public string? ShipName { get; set; }
        public int? TotalPrice { get; set; }
        public UserDto User { get; set; }
        public List<OrderDetailDto> OrderDetail { get; set; }
    }
    public class OrderDetailDto
    {
        public int Id { get; set; }
        public ProductDto Product { get; set; }
        public int? Quantity { get; set; }
        public int? Price { get; set; }
    }
    public class UpdateOrderDto
    {
        public string? Status { get; set; }
    }
}
