using BE_SaleHunter.Application.DTOs.Cart;

namespace BE_SaleHunter.Application.DTOs.Order
{
    public class TempOrderSession
    {
        public string CartId { get; set; }
        public List<CartItemDTO> Items { get; set; }
    }
}
