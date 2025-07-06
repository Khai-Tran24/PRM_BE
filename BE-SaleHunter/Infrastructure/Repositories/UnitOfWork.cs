using BE_SaleHunter.Core.Entities;
using BE_SaleHunter.Core.Interfaces;
using BE_SaleHunter.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;

namespace BE_SaleHunter.Infrastructure.Repositories
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly SaleHunterDbContext _context;
        private readonly ILogger<UnitOfWork> _logger;
        private readonly IServiceProvider _serviceProvider;
        private IDbContextTransaction? _transaction;
        private bool _disposed = false;

        public UnitOfWork(SaleHunterDbContext context, ILogger<UnitOfWork> logger, IServiceProvider serviceProvider)
        {
            _context = context;
            _logger = logger;
            _serviceProvider = serviceProvider;
        }

        public IUserRepository UserRepository => (IUserRepository)_serviceProvider.GetService(typeof(IUserRepository))!;
        public IStoreRepository StoreRepository => (IStoreRepository)_serviceProvider.GetService(typeof(IStoreRepository))!;
        public IProductRepository ProductRepository => (IProductRepository)_serviceProvider.GetService(typeof(IProductRepository))!;
        public IProductRatingRepository ProductRatingRepository => (IProductRatingRepository)_serviceProvider.GetService(typeof(IProductRatingRepository))!;

        public IGenericRepository<T> GenericRepository<T>() where T : BaseEntity
        {
            return (IGenericRepository<T>)_serviceProvider.GetService(typeof(IGenericRepository<T>))!;
        }        public async Task<int> CompleteAsync()
        {
            _logger.LogDebug("REPOSITORY LAYER - Completing Unit of Work transaction");
            var result = await _context.SaveChangesAsync();
            _logger.LogInformation("DATA CHANGE - Unit of Work completed, {ChangeCount} changes saved to database", result);
            return result;
        }

        public async Task BeginTransactionAsync()
        {
            _logger.LogDebug("REPOSITORY LAYER - Beginning database transaction");
            _transaction = await _context.Database.BeginTransactionAsync();
            _logger.LogInformation("DATABASE TRANSACTION - Transaction started");
        }

        public async Task CommitTransactionAsync()
        {
            if (_transaction != null)
            {
                _logger.LogDebug("REPOSITORY LAYER - Committing database transaction");
                await _transaction.CommitAsync();
                await _transaction.DisposeAsync();
                _transaction = null;
                _logger.LogInformation("DATABASE TRANSACTION - Transaction committed successfully");
            }
        }

        public async Task RollbackTransactionAsync()
        {
            if (_transaction != null)
            {
                _logger.LogWarning("REPOSITORY LAYER - Rolling back database transaction");
                await _transaction.RollbackAsync();
                await _transaction.DisposeAsync();
                _transaction = null;
                _logger.LogInformation("DATABASE TRANSACTION - Transaction rolled back");
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed && disposing)
            {
                _transaction?.Dispose();
                _context.Dispose();
            }

            _disposed = true;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}