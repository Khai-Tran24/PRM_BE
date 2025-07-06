using BE_SaleHunter.Core.Entities;
using BE_SaleHunter.Core.Interfaces;
using BE_SaleHunter.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BE_SaleHunter.Infrastructure.Repositories
{
    public class ProductRatingRepository : GenericRepository<ProductRating>, IProductRatingRepository
    {
        public ProductRatingRepository(SaleHunterDbContext context, ILogger<GenericRepository<ProductRating>> logger) : base(context, logger)
        {
        }

        public async Task<ProductRating?> GetUserRatingForProductAsync(long productId, long userId)
        {
            return await _dbSet
                .FirstOrDefaultAsync(pr => pr.ProductId == productId && pr.UserId == userId);
        }

        public async Task<IEnumerable<ProductRating>> GetProductRatingsAsync(long productId)
        {
            return await _dbSet
                .Where(pr => pr.ProductId == productId)
                .Include(pr => pr.User)
                .OrderByDescending(pr => pr.CreatedAt)
                .ToListAsync();
        }

        public async Task<double> GetAverageRatingAsync(long productId)
        {
            var ratings = await _dbSet
                .Where(pr => pr.ProductId == productId)
                .Select(pr => pr.Rating)
                .ToListAsync();

            return ratings.Any() ? ratings.Average() : 0.0;
        }

        public async Task<int> GetRatingCountAsync(long productId)
        {
            return await _dbSet
                .CountAsync(pr => pr.ProductId == productId);
        }
    }
}