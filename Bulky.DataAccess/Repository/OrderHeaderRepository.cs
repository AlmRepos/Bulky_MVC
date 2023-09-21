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
    public class OrderHeaderRepository : Repository<OrderHeader>, IOrderHeaderRepository
	{
        private readonly Context _db;

        public OrderHeaderRepository(Context db) : base(db)
        {
            _db = db;
        }

        public void Update(OrderHeader OrderHeader)
        {
            _db.Update(OrderHeader);
        }
    }
}
