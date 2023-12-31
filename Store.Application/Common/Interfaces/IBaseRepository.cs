using Microsoft.AspNetCore.Mvc.Rendering;
using Store.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Store.Application.Common.Interfaces
{
    public interface IBaseRepository<TEntity> where TEntity : BaseEntity
    {

        Task<IEnumerable<TEntity>> GetAllAsync(Expression<Func<TEntity, bool>>? filter = null, params Expression<Func<TEntity, object>>[] includeProperties);


        Task<TEntity> GetByIdAsync(Expression<Func<TEntity, bool>>? filter = null,params Expression<Func<TEntity, object>>[]? includeProperties);


        Task CreateAsync(TEntity entity);
        Task UpdateAsync(TEntity entity); 
        Task DeleteAsync(TEntity entity);

        Task DeleteByIdAsync(int id);

        Task<bool> AnyAsync(Expression<Func<TEntity, bool>> filter);

        Task<IEnumerable<SelectListItem>> GetAllListVillaAsync();




    }
}
