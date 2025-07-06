using BE_SaleHunter.Core.Entities;

namespace BE_SaleHunter.Core.Interfaces
{
    public interface IProductRatingRepository : IGenericRepository<ProductRating>
    {
        Task<ProductRating?> GetUserRatingForProductAsync(long productId, long userId);
        Task<IEnumerable<ProductRating>> GetProductRatingsAsync(long productId);
        Task<double> GetAverageRatingAsync(long productId);
        Task<int> GetRatingCountAsync(long productId);
    }
}
