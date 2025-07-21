using BE_SaleHunter.Application.DTOs;
using BE_SaleHunter.Core.Entities;

namespace BE_SaleHunter.Core.Interfaces
{
    public interface IOrderRepository : IGenericRepository<Order>
    {
        Task<IEnumerable<Order>> GetOrderOfStore(long storeId);
        Task<Order> GetOrderById(long storeId);
    }
}
