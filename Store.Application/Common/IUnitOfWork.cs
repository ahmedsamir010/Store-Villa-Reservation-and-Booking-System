using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Store.Application.Common.Interfaces;
using Store.Domain.Entities;

namespace Store.Application.Common
{
    public interface IUnitOfWork : IAsyncDisposable
    {

        Task<int> CompleteAsync();
        IBaseRepository<TEntity> Repository<TEntity>() where TEntity : BaseEntity;
   }
}
