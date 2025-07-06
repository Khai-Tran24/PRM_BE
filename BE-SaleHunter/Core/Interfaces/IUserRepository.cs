using BE_SaleHunter.Core.Entities;

namespace BE_SaleHunter.Core.Interfaces
{    public interface IUserRepository : IGenericRepository<User>
    {
        Task<User?> GetByEmailAsync(string email);
        Task<User?> GetByIdWithStoreAsync(long id);
        Task<User?> GetByRefreshTokenAsync(string refreshToken);
        Task<User?> GetByPasswordResetTokenAsync(string resetToken);
        Task<bool> EmailExistsAsync(string email);
        Task<User?> GetUserWithFavoritesAsync(long userId);
        Task<User?> GetUserWithViewHistoryAsync(long userId);
    }
}
