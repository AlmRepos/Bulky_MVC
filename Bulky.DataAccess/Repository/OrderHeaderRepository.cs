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
		public void UpdateStatus(int id, string orderStatus, string? PaymentStatus = null)
		{
			var OrderFromDb = _db.OrderHeaders.FirstOrDefault(o => o.Id == id);
			if(OrderFromDb != null)
			{
				OrderFromDb.OrderStatus = orderStatus;
				if(!string.IsNullOrEmpty(PaymentStatus))
				{
					OrderFromDb.PaymentStatus = PaymentStatus;
				}
			}
		}


		public void UpdateStripePaymentID(int id, string SessionId, string PaymentIntentId)
		{
			var OrderFromDb = _db.OrderHeaders.FirstOrDefault(o => o.Id == id);
			if(!string.IsNullOrEmpty(SessionId))
			{
				OrderFromDb.SessionId = SessionId;
			}
			if (!string.IsNullOrEmpty(PaymentIntentId))
			{
				OrderFromDb.PaymentIntentId = PaymentIntentId;
				OrderFromDb.PaymentDate = DateTime.Now;
			}
		}
	}
}
