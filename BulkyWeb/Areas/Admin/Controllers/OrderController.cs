using Bulky.DataAccess.IRepository;
using Bulky.Models;
using Bulky.Models.ViewModels;
using Bulky.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Stripe;
using Stripe.Checkout;
using System.Diagnostics;
using System.Security.Claims;

namespace BulkyWeb.Areas.Admin.Controllers
{
	[Area("Admin")]
    [Authorize]
	public class OrderController : Controller
	{
		private readonly IUnitOfWork _unitOfWork;

        [BindProperty]
        public OrderVM VM { get; set; }

        public OrderController(IUnitOfWork unitOfWork)
        {
			_unitOfWork = unitOfWork;
		}



        public IActionResult Index()
		{
			return View();
		}


        public IActionResult Details(int id)
        {
            VM = new()
            {
                OrderHeader = _unitOfWork.OrderHeader.GetOne(o => o.Id == id, includeproperties: "ApplicationUser"),
                OrderDetails = _unitOfWork.OrderDetail.GetAll(o => o.OrderHeaderId == id, includeproperties: "Product")
            };
            VM.OrderHeader.Id = id;

            return View(VM);
        }

        [HttpPost]
        [Authorize(Roles = SD.Role_Admin +","+SD.Role_Employee)]
        public IActionResult UpdateOrderDetail()
        {
            var orderFromDb = _unitOfWork.OrderHeader.GetOne(o => o.Id == VM.OrderHeader.Id);
            orderFromDb.Name = VM.OrderHeader.Name;
            orderFromDb.PhoneNumber = VM.OrderHeader.PhoneNumber;
            orderFromDb.State = VM.OrderHeader.State;
            orderFromDb.City = VM.OrderHeader.City;
            orderFromDb.StreetAddress = VM.OrderHeader.StreetAddress;
            orderFromDb.PostalCode = VM.OrderHeader.PostalCode;
            if (!string.IsNullOrEmpty(orderFromDb.Carrier))
                orderFromDb.Carrier = VM.OrderHeader.Carrier;
            if (!string.IsNullOrEmpty(orderFromDb.TrackingNumber))
                orderFromDb.Carrier = VM.OrderHeader.TrackingNumber;
            _unitOfWork.OrderHeader.Update(orderFromDb);
            _unitOfWork.Save();

            TempData["success"] = "Order Details Updateed Successfully";

            return RedirectToAction(nameof(Details), new {id = orderFromDb.Id});
        }




        [HttpPost]
        [Authorize(Roles = SD.Role_Admin + "," + SD.Role_Employee)]
        public IActionResult StartProcessing()
        {
            _unitOfWork.OrderHeader.UpdateStatus(VM.OrderHeader.Id, SD.Status_InProcess);
            _unitOfWork.Save();

            TempData["success"] = "Order Details Updateed Successfully";

            return RedirectToAction(nameof(Details), new { id = VM.OrderHeader.Id });
        }




        [HttpPost]
        [Authorize(Roles = SD.Role_Admin + "," + SD.Role_Employee)]
        public IActionResult ShipOrder()
        {
            var orderFromDb = _unitOfWork.OrderHeader.GetOne(o => o.Id == VM.OrderHeader.Id);

            orderFromDb.Carrier = VM.OrderHeader.Carrier;
            orderFromDb.TrackingNumber = VM.OrderHeader.TrackingNumber;
            orderFromDb.OrderStatus = SD.Status_Shipped;
            orderFromDb.ShippingDate = DateTime.Now;
           if(orderFromDb.PaymentStatus == SD.PaymentStatusDelayedPayment)
            {
                orderFromDb.PaymentDueDate = DateTime.Now.AddDays(30);
            }
            _unitOfWork.OrderHeader.Update(orderFromDb);
            _unitOfWork.Save();
            TempData["success"] = "Order Shipped Successfully";

            return RedirectToAction(nameof(Details), new { id = VM.OrderHeader.Id });
        }
        
        
        
        
        [HttpPost]
        [Authorize(Roles = SD.Role_Admin + "," + SD.Role_Employee)]
        public IActionResult CancelOrder()
        {
            var orderFromDb = _unitOfWork.OrderHeader.GetOne(o => o.Id == VM.OrderHeader.Id);
           if(orderFromDb.PaymentStatus == SD.PaymentStatusApproved)
            {
                var options = new RefundCreateOptions()
                {
                    Reason = RefundReasons.RequestedByCustomer,
                    PaymentIntent = orderFromDb.PaymentIntentId,
                };

                var service = new RefundService();
                Refund refund = service.Create(options);
                _unitOfWork.OrderHeader.UpdateStatus(orderFromDb.Id,SD.Status_Cancelled,SD.Status_Refunded);
            }
            else
            {
                _unitOfWork.OrderHeader.UpdateStatus(orderFromDb.Id, SD.Status_Cancelled, SD.Status_Refunded);
            }
            _unitOfWork.Save();
            TempData["success"] = "Order Cancelled Successfully";

            return RedirectToAction(nameof(Details), new { id = VM.OrderHeader.Id });
        }




        [ActionName("Details")]
        [HttpPost]
        public IActionResult DetailsPayNow()
        {
            VM.OrderHeader = _unitOfWork.OrderHeader.GetOne(o => o.Id == VM.OrderHeader.Id, includeproperties: "ApplicationUser");
            VM.OrderDetails = _unitOfWork.OrderDetail.GetAll(o => o.OrderHeaderId == VM.OrderHeader.Id, includeproperties: "Product");

            string domain = Request.Scheme + "://" + Request.Host.Value + "/";
            var options = new SessionCreateOptions
            {
                SuccessUrl = domain + $"Admin/Order/PaymentConfirmation?id={VM.OrderHeader.Id}",
                CancelUrl = domain + $"Admin/Order/Details?id={VM.OrderHeader.Id}",
                LineItems = new List<SessionLineItemOptions>(),
                Mode = "payment",
            };

            foreach (var item in VM.OrderDetails)
            {
                var sessionLineItem = new SessionLineItemOptions
                {
                    PriceData = new SessionLineItemPriceDataOptions
                    {
                        UnitAmount = (long)(item.Price * 100),
                        Currency = "usd",
                        ProductData = new SessionLineItemPriceDataProductDataOptions
                        {
                            Name = item.Product.Title
                        }
                    },
                    Quantity = item.Count
                };
                options.LineItems.Add(sessionLineItem);
            }

            var service = new SessionService();
            Session session = service.Create(options);

            _unitOfWork.OrderHeader.UpdateStripePaymentID(VM.OrderHeader.Id, session.Id, session.PaymentIntentId);
            _unitOfWork.Save();
            Response.Headers.Add("Location", session.Url);

            return new StatusCodeResult(303);
        }




        public IActionResult PaymentConfirmation(int id)
        {
            OrderHeader order = _unitOfWork.OrderHeader.GetOne(o => o.Id == id);
            if (order.PaymentStatus == SD.PaymentStatusDelayedPayment)
            {
                //this is a order by company
                var service = new SessionService();
                Session session = service.Get(order.SessionId);

                if (session.PaymentStatus.ToLower() == "paid")
                {
                    _unitOfWork.OrderHeader.UpdateStripePaymentID(order.Id, session.Id, session.PaymentIntentId);
                    _unitOfWork.OrderHeader.UpdateStatus(order.Id, order.OrderStatus, SD.PaymentStatusApproved);
                    _unitOfWork.Save();

                }

            }

            return View(id);
        }



        #region Api Calls
        [HttpGet]
		public IActionResult GetALl(string status)
		{
            IEnumerable<OrderHeader> orders;
            if(User.IsInRole(SD.Role_Admin) || User.IsInRole(SD.Role_Employee))
            {
                orders = _unitOfWork.OrderHeader.GetAll(includeproperties: "ApplicationUser").ToList();
            }
            else
            {
                var claimsIdentity = (ClaimsIdentity)User.Identity;
                var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;

                orders = _unitOfWork.OrderHeader.GetAll(u=>u.ApplicationUserId == userId,includeproperties: "ApplicationUser").ToList();
            }

            switch (status)
            {
                case "pending":
                    orders = orders.Where(o=>o.PaymentStatus == SD.PaymentStatusDelayedPayment);
                    break;
                case "inprocess":
                    orders = orders.Where(o => o.OrderStatus == SD.Status_InProcess);
                    break;
                case "completed":
                    orders = orders.Where(o => o.OrderStatus == SD.Status_Shipped);
                    break;
                case "approved":
                    orders = orders.Where(o => o.OrderStatus == SD.Status_Approved);
                    break;
                default:
                    break;
            }

            return Json(new { data = orders});
		}
		#endregion

	}
}
