using BE_SaleHunter.Core.Entities;
using BE_SaleHunter.Core.Interfaces;
using BE_SaleHunter.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Linq.Expressions;

namespace BE_SaleHunter.Infrastructure.Repositories
{
    public class GenericRepository<T> : IGenericRepository<T> where T : BaseEntity
    {
        protected readonly SaleHunterDbContext _context;
        protected readonly DbSet<T> _dbSet;
        protected readonly ILogger<GenericRepository<T>> _logger;

        public GenericRepository(SaleHunterDbContext context, ILogger<GenericRepository<T>> logger)
        {
            _context = context;
            _dbSet = context.Set<T>();
            _logger = logger;
        }

        public virtual async Task<T?> GetByIdAsync(long id)
        {
            return await _dbSet.FindAsync(id);
        }

        public virtual async Task<IEnumerable<T>> GetAllAsync()
        {
            return await _dbSet.ToListAsync();
        }

        public virtual async Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate)
        {
            return await _dbSet.Where(predicate).ToListAsync();
        }

        public virtual async Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate)
        {
            return await _dbSet.FirstOrDefaultAsync(predicate);
        }        public virtual async Task<T> AddAsync(T entity)
        {
            _logger.LogDebug("REPOSITORY LAYER - Adding entity of type {EntityType} with ID: {Id}", 
                typeof(T).Name, entity.Id);
            await _dbSet.AddAsync(entity);
            return entity;
        }

        public virtual async Task<IEnumerable<T>> AddRangeAsync(IEnumerable<T> entities)
        {
            var entitiesList = entities.ToList();
            _logger.LogDebug("REPOSITORY LAYER - Adding {Count} entities of type {EntityType}", 
                entitiesList.Count, typeof(T).Name);
            await _dbSet.AddRangeAsync(entitiesList);
            return entitiesList;
        }

        public virtual Task UpdateAsync(T entity)
        {
            _logger.LogDebug("REPOSITORY LAYER - Updating entity of type {EntityType} with ID: {Id}", 
                typeof(T).Name, entity.Id);
            
            // Track changes for detailed logging
            var entry = _context.Entry(entity);
            if (entry.State == EntityState.Modified)
            {
                var modifiedProperties = entry.Properties
                    .Where(p => p.IsModified)
                    .Select(p => new { Property = p.Metadata.Name, Original = p.OriginalValue, Current = p.CurrentValue })
                    .ToList();

                if (modifiedProperties.Any())
                {
                    _logger.LogInformation("DATA CHANGE - Entity {EntityType} ID: {Id} modified. Changes: {Changes}", 
                        typeof(T).Name, entity.Id, string.Join(", ", modifiedProperties.Select(p => 
                        $"{p.Property}: {p.Original} -> {p.Current}")));
                }
            }

            _dbSet.Update(entity);
            return Task.CompletedTask;
        }

        public virtual Task DeleteAsync(T entity)
        {
            _logger.LogInformation("DATA CHANGE - Deleting entity of type {EntityType} with ID: {Id}", 
                typeof(T).Name, entity.Id);
            _dbSet.Remove(entity);
            return Task.CompletedTask;
        }

        public virtual Task DeleteRangeAsync(IEnumerable<T> entities)
        {
            var entitiesList = entities.ToList();
            _logger.LogInformation("DATA CHANGE - Deleting {Count} entities of type {EntityType}. IDs: {Ids}", 
                entitiesList.Count, typeof(T).Name, string.Join(", ", entitiesList.Select(e => e.Id)));
            _dbSet.RemoveRange(entitiesList);
            return Task.CompletedTask;
        }

        public virtual async Task<int> CountAsync(Expression<Func<T, bool>>? predicate = null)
        {
            if (predicate == null)
                return await _dbSet.CountAsync();
            
            return await _dbSet.CountAsync(predicate);
        }

        public virtual async Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate)
        {
            return await _dbSet.AnyAsync(predicate);
        }
    }
}
