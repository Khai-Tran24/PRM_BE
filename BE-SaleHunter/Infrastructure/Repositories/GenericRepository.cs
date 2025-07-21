using BE_SaleHunter.Core.Entities;
using BE_SaleHunter.Core.Interfaces;
using BE_SaleHunter.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace BE_SaleHunter.Infrastructure.Repositories
{
    public class GenericRepository<T>(SaleHunterDbContext context, ILogger<GenericRepository<T>> logger)
        : IGenericRepository<T>
        where T : BaseEntity
    {
        protected readonly SaleHunterDbContext Context = context;
        protected readonly DbSet<T> DbSet = context.Set<T>();

        public virtual async Task<T?> GetByIdAsync(long id)
        {
            return await DbSet.FindAsync(id);
        }

        public virtual async Task<IEnumerable<T>> GetAllAsync()
        {
            return await DbSet.ToListAsync();
        }

        public virtual async Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate)
        {
            return await DbSet.Where(predicate).ToListAsync();
        }

        public virtual async Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate)
        {
            return await DbSet.FirstOrDefaultAsync(predicate);
        }

        public virtual async Task<T> AddAsync(T entity)
        {
            logger.LogDebug("REPOSITORY LAYER - Adding entity of type {EntityType} with ID: {Id}",
                typeof(T).Name, entity.Id);
            var response = await DbSet.AddAsync(entity);
            return response.Entity;
        }

        public virtual async Task<IEnumerable<T>> AddRangeAsync(IEnumerable<T> entities)
        {
            var entitiesList = entities.ToList();
            logger.LogDebug("REPOSITORY LAYER - Adding {Count} entities of type {EntityType}",
                entitiesList.Count, typeof(T).Name);
            await DbSet.AddRangeAsync(entitiesList);
            return entitiesList;
        }

        public virtual Task UpdateAsync(T entity)
        {
            logger.LogDebug("REPOSITORY LAYER - Updating entity of type {EntityType} with ID: {Id}",
                typeof(T).Name, entity.Id);

            // Track changes for detailed logging
            var entry = Context.Entry(entity);
            if (entry.State == EntityState.Modified)
            {
                var modifiedProperties = entry.Properties
                    .Where(p => p.IsModified)
                    .Select(p => new
                        { Property = p.Metadata.Name, Original = p.OriginalValue, Current = p.CurrentValue })
                    .ToList();

                if (modifiedProperties.Any())
                {
                    logger.LogInformation("DATA CHANGE - Entity {EntityType} ID: {Id} modified. Changes: {Changes}",
                        typeof(T).Name, entity.Id, string.Join(", ", modifiedProperties.Select(p =>
                            $"{p.Property}: {p.Original} -> {p.Current}")));
                }
            }

            DbSet.Update(entity);
            return Task.CompletedTask;
        }

        public virtual Task DeleteAsync(T entity)
        {
            logger.LogInformation("DATA CHANGE - Deleting entity of type {EntityType} with ID: {Id}",
                typeof(T).Name, entity.Id);
            DbSet.Remove(entity);
            return Task.CompletedTask;
        }

        public virtual Task DeleteRangeAsync(IEnumerable<T> entities)
        {
            var entitiesList = entities.ToList();
            logger.LogInformation("DATA CHANGE - Deleting {Count} entities of type {EntityType}. IDs: {Ids}",
                entitiesList.Count, typeof(T).Name, string.Join(", ", entitiesList.Select(e => e.Id)));
            DbSet.RemoveRange(entitiesList);
            return Task.CompletedTask;
        }

        public virtual async Task<int> CountAsync(Expression<Func<T, bool>>? predicate = null)
        {
            if (predicate == null)
                return await DbSet.CountAsync();

            return await DbSet.CountAsync(predicate);
        }

        public virtual async Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate)
        {
            return await DbSet.AnyAsync(predicate);
        }
    }
}