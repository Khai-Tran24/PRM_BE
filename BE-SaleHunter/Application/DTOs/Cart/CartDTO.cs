using System.ComponentModel.DataAnnotations;

namespace BE_SaleHunter.Application.DTOs.Cart
{
    public class CartDTO
    {
        public CartDTO() { }

        public CartDTO(string id)
        {
            Id = id;
            Items = [];
        }

        [Required]
        public required string Id { get; set; }
        public int TotalPrice { get; set; }

        public List<CartItemDTO> Items { get; set; } = [];
    }
}
