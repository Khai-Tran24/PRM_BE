using BE_SaleHunter.Core.Entities;
using BE_SaleHunter.Core.Interfaces;
using BE_SaleHunter.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BE_SaleHunter.Infrastructure.Repositories
{    public class StoreRepository : GenericRepository<Store>, IStoreRepository
    {
        private new readonly ILogger<StoreRepository> _logger;

        public StoreRepository(SaleHunterDbContext context, ILogger<StoreRepository> logger, ILogger<GenericRepository<Store>> genericLogger) : base(context, genericLogger)
        {
            _logger = logger;
        }

        public async Task<Store?> GetStoreWithProductsAsync(long storeId, int page = 1, int pageSize = 20)
        {
            _logger.LogDebug("REPOSITORY LAYER - GetStoreWithProductsAsync called for StoreId: {StoreId}, Page: {Page}, PageSize: {PageSize}", 
                storeId, page, pageSize);

            var store = await _dbSet
                .Include(s => s.User)
                .FirstOrDefaultAsync(s => s.Id == storeId);

            if (store != null)
            {
                _logger.LogDebug("Store found, loading products for StoreId: {StoreId}", storeId);
                // Load products with pagination
                var products = await _context.Products
                    .Where(p => p.StoreId == storeId)
                    .Include(p => p.Images)
                    .Include(p => p.Ratings)
                    .Include(p => p.Favorites)
                    .Include(p => p.Views)
                    .OrderByDescending(p => p.CreatedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                store.Products = products;
                _logger.LogDebug("Loaded {ProductCount} products for StoreId: {StoreId}", products.Count, storeId);
            }
            else
            {
                _logger.LogWarning("REPOSITORY LAYER - Store not found with ID: {StoreId}", storeId);
            }

            return store;
        }        public async Task<Store?> GetByUserIdAsync(long userId)
        {
            _logger.LogDebug("REPOSITORY LAYER - GetByUserIdAsync called for UserId: {UserId}", userId);

            var store = await _dbSet
                .Include(s => s.Products)
                    .ThenInclude(p => p.Images)
                .FirstOrDefaultAsync(s => s.UserId == userId);

            if (store != null)
            {
                _logger.LogDebug("Store found for UserId: {UserId}, StoreId: {StoreId}, StoreName: {StoreName}", 
                    userId, store.Id, store.Name);
            }
            else
            {
                _logger.LogDebug("No store found for UserId: {UserId}", userId);
            }

            return store;
        }

        public async Task<IEnumerable<Store>> GetStoresByLocationAsync(decimal latitude, decimal longitude, double radiusKm)
        {
            // Simple distance calculation using Haversine formula approximation
            // For more precise calculations, consider using PostGIS or similar extensions
            var stores = await _dbSet
                .Where(s => s.Latitude.HasValue && s.Longitude.HasValue)
                .ToListAsync();

            return stores.Where(s =>
            {
                var distance = CalculateDistance((double)latitude, (double)longitude, 
                    (double)s.Latitude!, (double)s.Longitude!);
                return distance <= radiusKm;
            });
        }

        public async Task<IEnumerable<Store>> SearchStoresAsync(string searchTerm)
        {
            return await _dbSet
                .Where(s => s.Name.Contains(searchTerm) || 
                           s.Category.Contains(searchTerm) ||
                           (s.Description != null && s.Description.Contains(searchTerm)))
                .Include(s => s.Products.Take(5))
                    .ThenInclude(p => p.Images)
                .ToListAsync();
        }

        public async Task<bool> UserHasStoreAsync(long userId)
        {
            return await _dbSet.AnyAsync(s => s.UserId == userId);
        }

        private static double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
        {
            const double earthRadius = 6371; // Earth's radius in kilometers

            var dLat = ToRadians(lat2 - lat1);
            var dLon = ToRadians(lon2 - lon1);

            var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                    Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
                    Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

            return earthRadius * c;
        }

        private static double ToRadians(double degrees)
        {
            return degrees * Math.PI / 180;
        }
    }
}
