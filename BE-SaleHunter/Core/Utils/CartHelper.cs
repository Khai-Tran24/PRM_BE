using System.Security.Claims;

namespace BE_SaleHunter.Core.Utils
{
    public static class CartHelper
    {
        public static string? GetCartId(HttpContext context)
        {
            // Ưu tiên user đã đăng nhập
            if (context.User.Identity?.IsAuthenticated == true)
            {
                return context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            }

            // Nếu chưa đăng nhập → lấy từ cookie
            if (context.Request.Cookies.TryGetValue("CartId", out var cartId))
            {
                return cartId;
            }
            return null;
        }

        public static string EnsureGuestCartId(HttpContext context)
        {
            var guestId = Guid.NewGuid().ToString();
            context.Response.Cookies.Append("CartId", guestId, new CookieOptions
            {
                Expires = DateTimeOffset.UtcNow.AddDays(30),
                HttpOnly = true,
                IsEssential = true
            });

            return guestId;
        }
    }
}
