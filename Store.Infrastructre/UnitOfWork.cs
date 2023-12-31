using Store.Application.Common;
using Store.Application.Common.Interfaces;
using Store.Domain.Entities;
using Store.Infrastructre.Data;
using Store.Infrastructre.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Store.Infrastructre
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly Dictionary<Type, object> _repositories = new Dictionary<Type, object>();
        private bool _disposed = false;


        public UnitOfWork(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }
        public IBaseRepository<TEntity> Repository<TEntity>() where TEntity : BaseEntity
        {
            var typeEntity = typeof(TEntity);
            if (!_repositories.TryGetValue(typeEntity, out var repository))
            {
                repository = new BaseRepository<TEntity>(_dbContext);
                _repositories.Add(typeEntity, repository);
            }
            return (IBaseRepository<TEntity>)repository;
        }
        public async Task<int> CompleteAsync() => await _dbContext.SaveChangesAsync();



        public async ValueTask DisposeAsync()
            {
            if (!_disposed)
            {
                await _dbContext.DisposeAsync();
                _disposed = true;
            }
            GC.SuppressFinalize(this);
        }

    }
}
