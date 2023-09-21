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
    public class OrderDetailRepository : Repository<OrderDetail>, IOrderDetailRepository
	{
        private readonly Context _db;

        public OrderDetailRepository(Context db) : base(db)
        {
            _db = db;
        }

        public void Update(OrderDetail OrderDetail)
        {
            _db.Update(OrderDetail);
        }
    }
}
