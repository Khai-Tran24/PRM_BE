using BE_SaleHunter.Application.DTOs.Cart;
using StackExchange.Redis;
using System.Text.Json;
namespace BE_SaleHunter.Application.Services
{
    public interface ICartService
    {
        Task<CartDTO?> GetCartAsync(long cartId);
        Task<CartDTO?> UpdateCartAsync(CartDTO cart);
        Task<bool> DeleteCartAsync(long cartId);
        Task<int?> GetCartItemsCount(long cartId);
        Task MergeCartsOnLoginAsync(long anonymousId, long userId);
    }
    public class CartService : ICartService
    {
        private readonly IDatabase _database;

        public CartService(IConnectionMultiplexer redis)
        {
            _database = redis.GetDatabase();
        }

        public async Task<CartDTO?> GetCartAsync(long cartId)
        {
            var cart = await _database.StringGetAsync(cartId.ToString());
            if (string.IsNullOrEmpty(cart))
            {
                return null;
            }
            var result = JsonSerializer.Deserialize<CartDTO?>(cart.ToString());
            return result;
        }

        public async Task<CartDTO?> UpdateCartAsync(CartDTO cart)
        {
            var updatedCart = await _database.StringSetAsync(
                cart.Id.ToString(),
                JsonSerializer.Serialize(cart),
                TimeSpan.FromDays(30)
            );
            if (!updatedCart)
                return null;
            return await GetCartAsync(long.Parse(cart.Id));
        }

        public Task<bool> DeleteCartAsync(long cartId)
        {
            return _database.KeyDeleteAsync(cartId.ToString());
        }

        public async Task<int?> GetCartItemsCount(long cartId)
        {
            var cartDto = await GetCartAsync(cartId);
            if (cartDto == null)
            {
                return null;
            }
            return cartDto.Items.Count();
        }
        public async Task MergeCartsOnLoginAsync(long anonymousId, long userId)
        {
            var anonCart = await GetCartAsync(anonymousId);
            var userCart = await GetCartAsync(userId);

            if (anonCart == null) return;

            if (userCart == null)
            {
                anonCart.Id = userId.ToString();
                await UpdateCartAsync(anonCart);
            }
            else
            {
                foreach (var item in anonCart.Items)
                {
                    var existing = userCart.Items.FirstOrDefault(x => x.Id == item.Id);
                    if (existing != null)
                        existing.Quantity += item.Quantity;
                    else
                        userCart.Items.Add(item);
                }
                await UpdateCartAsync(userCart);
            }

            await DeleteCartAsync(anonymousId);
        }

    }
}
