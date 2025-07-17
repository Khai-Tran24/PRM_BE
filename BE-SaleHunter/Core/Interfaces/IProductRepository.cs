using BE_SaleHunter.Core.Entities;

namespace BE_SaleHunter.Core.Interfaces
{
    public interface IProductRepository : IGenericRepository<Product>
    {
        Task<Product?> GetByIdWithAllDetailsAsync(long productId);
        Task<Product?> GetByIdWithStoreAsync(long productId);
        Task<IEnumerable<Product>> GetByStoreIdAsync(long storeId);
        Task<IEnumerable<Product>> SearchProductsAsync(string query, long? storeId = null, 
            string? category = null, decimal? minPrice = null, decimal? maxPrice = null);
        Task<IEnumerable<Product>> GetActiveProductsAsync();
        Task<IEnumerable<string>> GetDistinctCategoriesAsync();
        Task<IEnumerable<string>> GetDistinctBrandsAsync();
        Task<IEnumerable<Product>> GetRecentProductsAsync(int count);
        Task<IEnumerable<Product>> GetProductsByStoreIdsAsync(IEnumerable<long> storeIds, int count);
        Task<IEnumerable<Product>> GetOnSaleProductsAsync(int count);
    }
}
