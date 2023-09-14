using Bulky.DataAccess.Data;
using Bulky.DataAccess.Repository.IRepository;
using Bulky.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bulky.DataAccess.Repository
{
    public class ProductRepository : Repository<Product>, IProductRepository
    {
        private readonly Context _db;

        public ProductRepository (Context db) :base(db)
            {
            _db = db;
        }
        public void Update(Product product)
        {
            if(product != null)
            {
                Product? old = _db.Products.FirstOrDefault(p=>p.Id == product.Id);
                if(old != null)
                {
                    old.ISBN = product.ISBN;
                    old.Price = product.Price;
                    old.Price100 = product.Price50;
                    old.Price100 = product.Price100;
                    old.Title = product.Title;
                    old.ListPrice = product.ListPrice;
                    old.Author = product.Author;
                    old.CategoryID = product.CategoryID;
                    old.Description = product.Description;
                    if(old.ImageUrl != null)
                    {
                        old.ImageUrl = product.ImageUrl;
                    }
                }
            }
        }
    }
}
