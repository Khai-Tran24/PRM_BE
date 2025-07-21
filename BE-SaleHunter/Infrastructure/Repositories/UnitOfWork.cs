using BE_SaleHunter.Core.Entities;
using BE_SaleHunter.Core.Interfaces;
using BE_SaleHunter.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Storage;

namespace BE_SaleHunter.Infrastructure.Repositories;

public sealed class UnitOfWork(
    SaleHunterDbContext context,
    ILogger<UnitOfWork> logger,
    IServiceProvider serviceProvider)
    : IUnitOfWork
{
    private IDbContextTransaction? _transaction;
    private bool _disposed;

    public IUserRepository UserRepository => (IUserRepository)serviceProvider.GetService(typeof(IUserRepository))!;

    public IStoreRepository StoreRepository =>
        (IStoreRepository)serviceProvider.GetService(typeof(IStoreRepository))!;

    public IProductRepository ProductRepository =>
        (IProductRepository)serviceProvider.GetService(typeof(IProductRepository))!;

    public IProductRatingRepository ProductRatingRepository =>
        (IProductRatingRepository)serviceProvider.GetService(typeof(IProductRatingRepository))!;

    public IChatMessageRepository ChatMessageRepository =>
        (IChatMessageRepository)serviceProvider.GetService(typeof(IChatMessageRepository))!;

    public IChatConversationRepository ChatConversationRepository =>
        (IChatConversationRepository)serviceProvider.GetService(typeof(IChatConversationRepository))!;
    public IOrderRepository OrderRepository => (IOrderRepository)serviceProvider.GetService(typeof(IOrderRepository))!;

    public IGenericRepository<T> GenericRepository<T>() where T : BaseEntity
    {
        return (IGenericRepository<T>)serviceProvider.GetService(typeof(IGenericRepository<T>))!;
    }

    public async Task<int> CompleteAsync()
    {
        logger.LogDebug("REPOSITORY LAYER - Completing Unit of Work transaction");
        var result = await context.SaveChangesAsync();
        logger.LogInformation("DATA CHANGE - Unit of Work completed, {ChangeCount} changes saved to database",
            result);
        return result;
    }

    public async Task BeginTransactionAsync()
    {
        logger.LogDebug("REPOSITORY LAYER - Beginning database transaction");
        _transaction = await context.Database.BeginTransactionAsync();
        logger.LogInformation("DATABASE TRANSACTION - Transaction started");
    }

    public async Task CommitTransactionAsync()
    {
        if (_transaction != null)
        {
            logger.LogDebug("REPOSITORY LAYER - Committing database transaction");
            await _transaction.CommitAsync();
            await _transaction.DisposeAsync();
            _transaction = null;
            logger.LogInformation("DATABASE TRANSACTION - Transaction committed successfully");
        }
    }

    public async Task RollbackTransactionAsync()
    {
        if (_transaction != null)
        {
            logger.LogWarning("REPOSITORY LAYER - Rolling back database transaction");
            await _transaction.RollbackAsync();
            await _transaction.DisposeAsync();
            _transaction = null;
            logger.LogInformation("DATABASE TRANSACTION - Transaction rolled back");
        }
    }

    private void Dispose(bool disposing)
    {
        if (!_disposed && disposing)
        {
            _transaction?.Dispose();
            context.Dispose();
        }

        _disposed = true;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}