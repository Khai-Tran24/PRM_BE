using BE_SaleHunter.Core.Entities;

namespace BE_SaleHunter.Core.Interfaces
{
    public interface IStoreRepository : IGenericRepository<Store>
    {
        Task<Store?> GetStoreWithProductsAsync(long storeId, int page = 1, int pageSize = 20);
        Task<Store?> GetByUserIdAsync(long userId);
        Task<IEnumerable<Store>> GetStoresByLocationAsync(decimal latitude, decimal longitude, double radiusKm);
        Task<IEnumerable<Store>> SearchStoresAsync(string searchTerm);
        Task<bool> UserHasStoreAsync(long userId);
    }
}
