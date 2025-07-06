using BE_SaleHunter.Core.Entities;
using BE_SaleHunter.Core.Interfaces;
using BE_SaleHunter.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BE_SaleHunter.Infrastructure.Repositories
{
    public class UserRepository : GenericRepository<User>, IUserRepository
    {
        public UserRepository(SaleHunterDbContext context, ILogger<GenericRepository<User>> logger) : base(context, logger)
        {
        }

        public async Task<User?> GetByEmailAsync(string email)
        {
            return await _dbSet.FirstOrDefaultAsync(u => u.Email == email);
        }

        public async Task<User?> GetByIdWithStoreAsync(long id)
        {
            return await _dbSet
                .Include(u => u.Store)
                .FirstOrDefaultAsync(u => u.Id == id);
        }

        public async Task<bool> EmailExistsAsync(string email)
        {
            return await _dbSet.AnyAsync(u => u.Email == email);
        }

        public async Task<User?> GetUserWithFavoritesAsync(long userId)
        {
            return await _dbSet
                .Include(u => u.Favorites)
                    .ThenInclude(f => f.Product)
                        .ThenInclude(p => p.Images)
                .Include(u => u.Favorites)
                    .ThenInclude(f => f.Product)
                        .ThenInclude(p => p.Store)
                .FirstOrDefaultAsync(u => u.Id == userId);
        }

        public async Task<User?> GetUserWithViewHistoryAsync(long userId)
        {
            return await _dbSet
                .Include(u => u.ProductViews)
                    .ThenInclude(pv => pv.Product)
                        .ThenInclude(p => p.Images)                .Include(u => u.ProductViews)
                    .ThenInclude(pv => pv.Product)
                        .ThenInclude(p => p.Store)
                .FirstOrDefaultAsync(u => u.Id == userId);
        }

        public async Task<User?> GetByRefreshTokenAsync(string refreshToken)
        {
            return await _dbSet.FirstOrDefaultAsync(u => u.RefreshToken == refreshToken);
        }

        public async Task<User?> GetByPasswordResetTokenAsync(string resetToken)
        {
            return await _dbSet.FirstOrDefaultAsync(u => u.PasswordResetToken == resetToken);
        }
    }
}
