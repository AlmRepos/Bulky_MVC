using Bulky.DataAccess.Data;
using Bulky.DataAccess.Repository.IRepository;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Bulky.DataAccess.Repository
{
    public class Repository<T> : IRepository<T> where T : class
    {
        private readonly Context _db;
        internal DbSet<T> dbset;

        public Repository(Context db)
        {
            _db = db;
            dbset = _db.Set<T>();
            _db.Products.Include(c => c.category)./*as if you need to inclue more properties*/Include(c => c.CategoryID);
            //_db.Categories == dbset
        }
        public void Add(T entity)
        {
            dbset.Add(entity);
        }

        public IEnumerable<T> GetAll(string? includeproperties = null)
        {
            IQueryable<T> query = dbset;
            if (!string.IsNullOrEmpty(includeproperties))
            {
                foreach (var include in includeproperties
                    .Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    query = query.Include(include);
                }
            }
            return query.ToList();

        }

        public T GetOne(Expression<Func<T, bool>> filter, string? includeproperties = null)
        {
            IQueryable<T> query = dbset;
            query = query.Where(filter);
            if (!string.IsNullOrEmpty(includeproperties))
            {
                foreach (string include in includeproperties
                    .Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    query = query.Include(include);
                }
            }
            return query.FirstOrDefault();
        }

        public void Remove(T entity)
        {
            dbset.Remove(entity);
        }

        public void RemoveRange(IEnumerable<T> entities)
        {
            dbset.RemoveRange(entities);
        }
    }
}
