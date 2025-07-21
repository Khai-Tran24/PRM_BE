using BE_SaleHunter.Application.DTOs.Order;
using BE_SaleHunter.Application.Services;
using BE_SaleHunter.Core.Entities;
using BE_SaleHunter.Infrastructure.Data;
using Microsoft.Extensions.Caching.Memory;
using Net.payOS;

namespace BE_SaleHunter.Infrastructure.BackgroundJobs
{
    public class PaymentProcessingService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IMemoryCache _memoryCache;
        private readonly PayOS _payOS;
        public PaymentProcessingService(IServiceProvider serviceProvider, IMemoryCache memoryCache, PayOS payOS)
        {
            _serviceProvider = serviceProvider;
            _memoryCache = memoryCache;
            _payOS = payOS;
        }
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // This method will be called when the service starts
            while (!stoppingToken.IsCancellationRequested)
            {
                await CheckPendingOrders(stoppingToken);
                await Task.Delay(3000, stoppingToken); // Simulate work
            }
        }
        private async Task CheckPendingOrders(CancellationToken token)
        {
            var keys = GetAllCacheKeys(); // You need to track keys
            foreach (var key in keys)
            {
                if (_memoryCache.TryGetValue(key, out TempOrderSession session))
                {
                    try
                    {
                        var info = await _payOS.getPaymentLinkInformation(long.Parse(key));
                        if (info.status == "PAID")
                        {
                            using var scope = _serviceProvider.CreateScope();
                            var db = scope.ServiceProvider.GetRequiredService<SaleHunterDbContext>();
                            var orderDetails = new List<OrderDetail>();
                            foreach (var i in session.Items)
                            {
                                var product = await db.Products.FindAsync((long)i.Id);
                                if (product != null)
                                {
                                    orderDetails.Add(new OrderDetail
                                    {
                                        Product = product,
                                        Quantity = i.Quantity,
                                        Price = (int)i.Price
                                    });
                                }
                            }
                            var user = await db.Users.FindAsync(long.Parse(session.CartId));
                            // Save order
                            var order = new Order
                            {
                                User = user,
                                CreatedAt = DateTime.UtcNow,
                                TotalPrice = info.amount,
                                Status = info.status,
                                OrderDetails = orderDetails,
                            };
                            db.Orders.Add(order);
                            await db.SaveChangesAsync(token);

                            _memoryCache.Remove(key);
                            var cartService = scope.ServiceProvider.GetRequiredService<ICartService>();
                            await cartService.DeleteCartAsync(long.Parse(session.CartId));
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Failed to process order {key}: {ex.Message}");
                    }
                }
            }
        }
        private List<string> GetAllCacheKeys()
        {
            // Best practice: store keys in a List<string> in memory
            return KeyTracker.OrderKeys.ToList();
        }
    }
}
