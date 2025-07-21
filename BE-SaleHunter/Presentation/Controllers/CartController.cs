using BE_SaleHunter.Application.DTOs.Cart;
using BE_SaleHunter.Application.Services;
using BE_SaleHunter.Core.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using System.Runtime.InteropServices;
using System.Security.Claims;

namespace BE_SaleHunter.Presentation.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class CartController : ControllerBase
    {
        private readonly ICartService _cartService;

        public CartController(ICartService cartService)
        {
            _cartService = cartService;
        }
        //private string GetCartId()
        //{
        //    var id = CartHelper.GetCartId(HttpContext);
        //    if (id == null)
        //    {
        //        id = CartHelper.EnsureGuestCartId(HttpContext);
        //    }
        //    return id;
        //}

        [HttpGet]
        public async Task<IActionResult> GetCart()
        {
            var cartId = GetCurrentUserId();
            var cart = await _cartService.GetCartAsync(cartId.Value);
            int totalPrice = 0;
            foreach (var item in cart?.Items ?? new List<CartItemDTO>())
            {
                totalPrice += (int)item.Price * item.Quantity;
            }
            return Ok(cart ?? new CartDTO { Id = cartId.ToString(), TotalPrice = totalPrice, Items = new List<CartItemDTO>() });
        }

        [HttpPost]
        public async Task<IActionResult> AddToCart([FromBody] CartItemDTO item)
        {
            var cartId = GetCurrentUserId();
            var cart = await _cartService.GetCartAsync(cartId.Value);

            if (cart == null)
            {
                cart = new CartDTO
                {
                    Id = cartId.ToString(),
                    TotalPrice = (int)item.Price * item.Quantity,
                    Items = new List<CartItemDTO> { item }
                };
            }
            else
            {
                var existingItem = cart.Items.FirstOrDefault(i => i.Id == item.Id);
                if (existingItem != null)
                {
                    // Nếu sản phẩm đã có trong giỏ, tăng số lượng
                    existingItem.Quantity += item.Quantity;
                }
                else
                {
                    cart.Items.Add(item);
                }
            }

            var updated = await _cartService.UpdateCartAsync(cart);
            return Ok(updated);
        }

        [HttpDelete("{productId}")]
        public async Task<IActionResult> RemoveItem(int productId)
        {
            var cartId = GetCurrentUserId()!;
            var cart = await _cartService.GetCartAsync(cartId.Value);
            if (cart == null) return NotFound();

            cart.Items.RemoveAll(x => x.Id == productId);
            var updated = await _cartService.UpdateCartAsync(cart);
            return Ok(updated);
        }

        [HttpGet("count")]
        public async Task<IActionResult> GetItemCount()
        {
            var cartId = GetCurrentUserId();
            var count = await _cartService.GetCartItemsCount(cartId.Value);
            return Ok(count ?? 0);
        }
        private long? GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (long.TryParse(userIdClaim, out var userId))
            {
                return userId;
            }

            return null;
        }
    }
}
