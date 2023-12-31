using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Store.Application.Common.Interfaces;
using Store.Domain.Entities;
using Store.Infrastructre.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Store.Infrastructre.Repositories
{
    public class BaseRepository<TEntity> : IBaseRepository<TEntity> where TEntity : BaseEntity
    {
        private readonly ApplicationDbContext _dbContext;
        internal DbSet<TEntity> _dbSet;
        public BaseRepository(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
            _dbSet = _dbContext.Set<TEntity>();
        }

        public async Task<bool> AnyAsync(Expression<Func<TEntity, bool>> filter)
        {
        
           return await _dbSet.AnyAsync(filter);
        }

        public async Task CreateAsync(TEntity entity)
        {
            await _dbSet.AddAsync(entity);
        }

        public async Task DeleteAsync(TEntity entity)
        {
            _dbSet.Remove(entity);
        }

        public async Task DeleteByIdAsync(int id)
        {
            if (id <= 0)
            {
                return;
            }
            var entityToDelete = await _dbSet.FindAsync(id);
            if(entityToDelete is not null) 
            {
                _dbSet.Remove(entityToDelete);
            }

        }

        public async Task<TEntity> GetByIdAsync(Expression<Func<TEntity, bool>>? filter = null,params Expression<Func<TEntity, object>>[] includeProperties)
        {
            IQueryable<TEntity> query = _dbSet;

            if (filter != null)
            {
                query = query.Where(filter);
            }

            if (includeProperties != null)
            {
                foreach (var include in includeProperties)
                {
                    query = query.Include(include);
                }
            }

            return await query.FirstOrDefaultAsync();
        }
        public async Task<TEntity> GetByIdAsync(
    Expression<Func<TEntity, bool>> filter,
    Expression<Func<TEntity, object>> includeProperty1,
    Expression<Func<TEntity, object>> includeProperty2)
        {
            return await GetByIdAsync(filter, new[] { includeProperty1, includeProperty2 });
        }

        public async Task<IEnumerable<TEntity>> GetAllAsync(
        Expression<Func<TEntity, bool>>? filter = null,
        params Expression<Func<TEntity, object>>[] includeProperties)
        {
            IQueryable<TEntity> query = _dbSet;

            if (filter != null)
            {
                query = query.Where(filter);
            }

            if (includeProperties != null)
            {
                foreach (var include in includeProperties)
                {
                    query = query.Include(include);
                }
            }

            return await query.ToListAsync();
        }



        public async Task<IEnumerable<SelectListItem>> GetAllListVillaAsync()
        {
            var entities = await _dbContext.Villas.ToListAsync();

            var selectListItems = entities.Select(villa => new SelectListItem
            {
                Value = villa.Id.ToString(),
                Text = villa.Name 
            });

            return selectListItems;
        }

        public async Task UpdateAsync(TEntity entity)
        {
            _dbSet.Update(entity);
            await _dbContext.SaveChangesAsync(); 
        }


    }
}
