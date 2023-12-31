﻿using Bulky.DataAccess.Repository.IRepository;
using Bulky.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bulky.DataAccess.Repository.IRepository
{
    public interface IOrderHeaderRepository : IRepository<OrderHeader>
    {
        void Update(OrderHeader OrderHeader);

        void UpdateStatus(int id,string orderStatus,string? PaymentStatus = null);

        void UpdateStripePaymentID(int id, string SessionId, string PaymentIntentId);
    }
}
