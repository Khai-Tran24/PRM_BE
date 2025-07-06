using BE_SaleHunter.Core.Entities;

namespace BE_SaleHunter.Core.Interfaces
{
    public interface IUnitOfWork : IDisposable
    {
        IUserRepository UserRepository { get; }
        IStoreRepository StoreRepository { get; }
        IProductRepository ProductRepository { get; }
        IProductRatingRepository ProductRatingRepository { get; }
        IGenericRepository<T> GenericRepository<T>() where T : BaseEntity;
        Task<int> CompleteAsync();
        Task BeginTransactionAsync();
        Task CommitTransactionAsync();
        Task RollbackTransactionAsync();
    }
}
