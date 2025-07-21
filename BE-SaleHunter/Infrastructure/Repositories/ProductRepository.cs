using Microsoft.EntityFrameworkCore;
using BE_SaleHunter.Core.Entities;
using BE_SaleHunter.Core.Interfaces;
using BE_SaleHunter.Infrastructure.Data;

namespace BE_SaleHunter.Infrastructure.Repositories
{
    public class ProductRepository : GenericRepository<Product>, IProductRepository
    {
        public ProductRepository(SaleHunterDbContext context, ILogger<GenericRepository<Product>> logger) : base(
            context, logger)
        {
        }

        public async Task<Product?> GetByIdWithAllDetailsAsync(long productId)
        {
            return await Context.Products
                .Include(p => p.Store)
                .Include(p => p.Images)
                .Include(p => p.PriceHistory)
                .Include(p => p.Ratings)
                .Include(p => p.Views)
                .Include(p => p.Favorites)
                .FirstOrDefaultAsync(p => p.Id == productId);
        }

        public async Task<Product?> GetByIdWithStoreAsync(long productId)
        {
            return await Context.Products
                .Include(p => p.Store)
                .FirstOrDefaultAsync(p => p.Id == productId);
        }

        public async Task<IEnumerable<Product>> GetByStoreIdAsync(long storeId)
        {
            return await Context.Products
                .Include(p => p.Images)
                .Include(p => p.PriceHistory)
                .Where(p => p.StoreId == storeId)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<Product>> SearchProductsAsync(
            int page, int size,
            string query, long? storeId = null,
            string? category = null, decimal? minPrice = null, decimal? maxPrice = null,
            string? sortBy = "popularity", string? brand = null
        )
        {
            var productsQuery = Context.Products
                .Include(p => p.Store)
                .Include(p => p.Images)
                .Include(p => p.PriceHistory)
                .AsQueryable();
            
            // Text search
            if (!string.IsNullOrWhiteSpace(query))
            {
                productsQuery = productsQuery.Where(p =>
                    p.Name.Contains(query) ||
                    (p.Description != null && p.Description.Contains(query)) ||
                    (p.Brand != null && p.Brand.Contains(query)));
            }

            // Store filter
            if (storeId.HasValue)
            {
                productsQuery = productsQuery.Where(p => p.StoreId == storeId.Value);
            }

            // Category filter
            if (!string.IsNullOrWhiteSpace(category))
            {
                productsQuery = productsQuery.Where(p => p.Category == category);
            }

            // Brand filter
            if (!string.IsNullOrWhiteSpace(brand))
            {
                productsQuery = productsQuery.Where(p => p.Brand == brand);
            }

            // Price filters
            if (minPrice.HasValue)
            {
                productsQuery = productsQuery.Where(p => p.Price >= minPrice.Value);
            }

            if (maxPrice.HasValue)
            {
                productsQuery = productsQuery.Where(p => p.Price <= maxPrice.Value);
            }

            // Apply sorting
            productsQuery = sortBy?.ToLower() switch
            {
                "price_asc" => productsQuery.OrderBy(p => p.Price),
                "price_dsc" => productsQuery.OrderByDescending(p => p.Price),
                "newest" => productsQuery.OrderByDescending(p => p.CreatedAt),
                "best_deal" => productsQuery.OrderByDescending(p => p.SalePercent),
                "nearest_store" => productsQuery.OrderBy(p => p.Store.Name), // Placeholder for location-based sorting
                _ => productsQuery
            };

            var result = await productsQuery
                .Skip(page * size)
                .Take(size)
                .ToListAsync();

            result = sortBy?.ToLower() switch
            {
                "rating" => result.OrderBy(p => p.AverageRating).ToList(),
                "popularity" => result.OrderByDescending(p => p.TotalViews).ToList(),
                _ => result.OrderByDescending(p => p.TotalViews).ToList() // Default to popularity
            };

            return result;
        }

        public async Task<IEnumerable<Product>> GetActiveProductsAsync()
        {
            return await Context.Products
                .Include(p => p.Store)
                .Include(p => p.Images)
                .Include(p => p.PriceHistory)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<string>> GetDistinctCategoriesAsync()
        {
            return await Context.Products
                .Where(p => !string.IsNullOrEmpty(p.Category))
                .Select(p => p.Category)
                .Distinct()
                .OrderBy(c => c)
                .ToListAsync();
        }

        public async Task<IEnumerable<string>> GetDistinctBrandsAsync()
        {
            return await Context.Products
                .Where(p => !string.IsNullOrEmpty(p.Brand))
                .Select(p => p.Brand!)
                .Distinct()
                .OrderBy(b => b)
                .ToListAsync();
        }

        public async Task<IEnumerable<Product>> GetRecentProductsAsync(int count)
        {
            return await Context.Products
                .Include(p => p.Store)
                .OrderByDescending(p => p.CreatedAt)
                .Take(count)
                .ToListAsync();
        }

        public async Task<IEnumerable<Product>> GetProductsByStoreIdsAsync(IEnumerable<long> storeIds, int count)
        {
            return await Context.Products
                .Include(p => p.Store)
                .Include(p => p.Ratings)
                .Where(p => storeIds.Contains(p.StoreId))
                .OrderByDescending(p => p.CreatedAt)
                .Take(count)
                .ToListAsync();
        }

        public async Task<IEnumerable<Product>> GetOnSaleProductsAsync(int count)
        {
            return await Context.Products
                .Include(p => p.Store)
                .Include(p => p.Ratings)
                .Include(p => p.Images)
                .Where(p => p.SalePercent > 0)
                .OrderByDescending(p => p.SalePercent)
                .Take(count)
                .ToListAsync();
        }

        public async Task<IEnumerable<Product>> GetAllProductsWithDetailAsync()
        {
            return await Context.Products
                .Include(p => p.Store)
                .Include(p => p.Images)
                .Include(p => p.PriceHistory)
                .Include(p => p.Ratings)
                .Include(p => p.Views)
                .Include(p => p.Favorites)
                .ToListAsync();
        }

        public async Task<int> GetTotalViewsForProducts(List<long> productIds)
        {
            if (productIds == null || !productIds.Any())
            {
                return 0;
            }

            return await Context.ProductViews
                .Where(v => productIds.Contains(v.ProductId))
                .CountAsync();
        }
    }
}
