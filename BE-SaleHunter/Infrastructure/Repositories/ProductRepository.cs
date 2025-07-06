using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using BE_SaleHunter.Core.Entities;
using BE_SaleHunter.Core.Interfaces;
using BE_SaleHunter.Infrastructure.Data;

namespace BE_SaleHunter.Infrastructure.Repositories
{
    public class ProductRepository : GenericRepository<Product>, IProductRepository
    {
        public ProductRepository(SaleHunterDbContext context, ILogger<GenericRepository<Product>> logger) : base(context, logger)
        {
        }

        public async Task<Product?> GetByIdWithAllDetailsAsync(long productId)
        {
            return await _context.Products
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
            return await _context.Products
                .Include(p => p.Store)
                .FirstOrDefaultAsync(p => p.Id == productId);
        }        public async Task<IEnumerable<Product>> GetByStoreIdAsync(long storeId)
        {
            return await _context.Products
                .Include(p => p.Images)
                .Include(p => p.PriceHistory)
                .Where(p => p.StoreId == storeId)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();
        }        public async Task<IEnumerable<Product>> SearchProductsAsync(string query, long? storeId = null, 
            string? category = null, decimal? minPrice = null, decimal? maxPrice = null)
        {
            var productsQuery = _context.Products
                .Include(p => p.Store)
                .Include(p => p.Images)
                .Include(p => p.PriceHistory)
                .AsQueryable();            if (!string.IsNullOrWhiteSpace(query))
            {
                productsQuery = productsQuery.Where(p => 
                    p.Name.Contains(query) || 
                    (p.Description != null && p.Description.Contains(query)) ||
                    (p.Brand != null && p.Brand.Contains(query)));
            }

            if (storeId.HasValue)
            {
                productsQuery = productsQuery.Where(p => p.StoreId == storeId.Value);
            }

            if (!string.IsNullOrWhiteSpace(category))
            {
                productsQuery = productsQuery.Where(p => p.Category == category);
            }

            if (minPrice.HasValue)
            {
                productsQuery = productsQuery.Where(p => p.Price >= minPrice.Value);
            }

            if (maxPrice.HasValue)
            {
                productsQuery = productsQuery.Where(p => p.Price <= maxPrice.Value);
            }

            return await productsQuery
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();
        }        public async Task<IEnumerable<Product>> GetActiveProductsAsync()
        {
            return await _context.Products
                .Include(p => p.Store)
                .Include(p => p.Images)
                .Include(p => p.PriceHistory)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<string>> GetDistinctCategoriesAsync()
        {
            return await _context.Products
                .Where(p => !string.IsNullOrEmpty(p.Category))
                .Select(p => p.Category)
                .Distinct()
                .OrderBy(c => c)
                .ToListAsync();
        }        public async Task<IEnumerable<string>> GetDistinctBrandsAsync()
        {
            return await _context.Products
                .Where(p => !string.IsNullOrEmpty(p.Brand))
                .Select(p => p.Brand!)
                .Distinct()
                .OrderBy(b => b)
                .ToListAsync();
        }
    }
}
