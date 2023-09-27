using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bulky.Utility
{
    public static class SD
    {
        public const string Role_Customer = "Customer";
        public const string Role_Admin = "Admin";
        public const string Role_Company = "Company";
        public const string Role_Employee = "Employee";

		public const string Status_Pending = "Pending";
		public const string Status_Approved = "Approved";
		public const string Status_InProcess = "Processing";
		public const string Status_Shipped = "Shipped";
		public const string Status_Cancelled = "Cancelled";
		public const string Status_Refunded = "Refunded";

		public const string PaymentStatusPending = "Pending";
		public const string PaymentStatusApproved = "Approved";
		public const string PaymentStatusDelayedPayment = "Approved For Delayed Payment";
		public const string PaymentStatusRejected = "Rejected";

		public const string SessionCart = "SessionShoppingCart";
    }
}
