using BE_SaleHunter.Application.DTOs;
using BE_SaleHunter.Core.Entities;
using BE_SaleHunter.Core.Interfaces;
using BE_SaleHunter.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualBasic;

namespace BE_SaleHunter.Infrastructure.Repositories
{
    public class OrderRepository : GenericRepository<Order>, IOrderRepository
    {
        public OrderRepository(SaleHunterDbContext context, ILogger<GenericRepository<Order>> logger) : base(context, logger)
        {
        }

        public async Task<IEnumerable<Order>> GetOrderOfStore(long storeId)
        {
            return await Context.Orders
                .Where(o => o.OrderDetails.Any(od => od.Product.StoreId == storeId))
                .ToListAsync();

        }
        public async Task<Order> GetOrderById(long storeId)
        {
            return await Context.Orders
                .Include(o => o.OrderDetails)
                .Include(o => o.User)
                .FirstOrDefaultAsync(c => c.Id == storeId);
        }
    }
}
