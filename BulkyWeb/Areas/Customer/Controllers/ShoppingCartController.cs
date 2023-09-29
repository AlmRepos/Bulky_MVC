using Bulky.DataAccess.IRepository;
using Bulky.Models;
using Bulky.Models.ViewModels;
using Bulky.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Stripe.Checkout;
using System.Security.Claims;

namespace BulkyWeb.Areas.Customer.Controllers
{
    [Area("Customer")]
    [Authorize]
    public class ShoppingCartController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        [BindProperty]
        public ShoppingCartVM ShoppingCartVM { get; set; }




        public ShoppingCartController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        

        public IActionResult Index()
        {
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;

            ShoppingCartVM = new ()
            {
                ShoppingCartList = _unitOfWork.ShoppingCart.GetAll(s=>s.ApplicationUserId == userId,
                includeproperties: "product"),
                OrderHeader = new()
            };

            foreach(var cart in ShoppingCartVM.ShoppingCartList)
            {
                cart.Price = GetPriceBasedOnQuantity(cart);
                ShoppingCartVM.OrderHeader.OrderTotal += (cart.Price * cart.Count);
            }

            return View(ShoppingCartVM);
        }



        public IActionResult Summary()
        {
			var claimsIdentity = (ClaimsIdentity)User.Identity;
			var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;

			ShoppingCartVM = new()
			{
				ShoppingCartList = _unitOfWork.ShoppingCart.GetAll(s => s.ApplicationUserId == userId,
				includeproperties: "product"),
				OrderHeader = new()
			};
            ShoppingCartVM.OrderHeader.ApplicationUser = _unitOfWork.ApplicationUser.GetOne(u => u.Id == userId);

            ShoppingCartVM.OrderHeader.Name = ShoppingCartVM.OrderHeader.ApplicationUser.Name;
            ShoppingCartVM.OrderHeader.PhoneNumber = ShoppingCartVM.OrderHeader.ApplicationUser.PhoneNumber;
            ShoppingCartVM.OrderHeader.State = ShoppingCartVM.OrderHeader.ApplicationUser.State;
            ShoppingCartVM.OrderHeader.City = ShoppingCartVM.OrderHeader.ApplicationUser.City;
            ShoppingCartVM.OrderHeader.StreetAddress = ShoppingCartVM.OrderHeader.ApplicationUser.StreetAddress;
            ShoppingCartVM.OrderHeader.PostalCode = ShoppingCartVM.OrderHeader.ApplicationUser.PostalCode;
			foreach (var cart in ShoppingCartVM.ShoppingCartList)
			{
				cart.Price = GetPriceBasedOnQuantity(cart);
				ShoppingCartVM.OrderHeader.OrderTotal += (cart.Price * cart.Count);
			}


			return View(ShoppingCartVM);
        }




        [HttpPost]
        [ActionName("Summary")]
		public IActionResult SummaryPost(ShoppingCartVM VM)
		{
			var claimsIdentity = (ClaimsIdentity)User.Identity;
			var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;

			ShoppingCartVM.ShoppingCartList = _unitOfWork.ShoppingCart.
                GetAll(s => s.ApplicationUserId == userId,includeproperties: "product");

            ShoppingCartVM.OrderHeader.OrderDate = DateTime.Now;
            ShoppingCartVM.OrderHeader.ApplicationUserId = userId;

            ApplicationUser applicationUser = _unitOfWork.ApplicationUser.GetOne(u => u.Id == userId);

			foreach (var cart in ShoppingCartVM.ShoppingCartList)
			{
				cart.Price = GetPriceBasedOnQuantity(cart);
				ShoppingCartVM.OrderHeader.OrderTotal += (cart.Price * cart.Count);
			}

            if(applicationUser.CompanyId.GetValueOrDefault() == 0)
            {
                // it is a regular user
                ShoppingCartVM.OrderHeader.PaymentStatus = SD.PaymentStatusPending;
                ShoppingCartVM.OrderHeader.OrderStatus = SD.Status_Pending;
            }
            else
            {
				// it is a company user
				ShoppingCartVM.OrderHeader.PaymentStatus = SD.PaymentStatusDelayedPayment;
				ShoppingCartVM.OrderHeader.OrderStatus = SD.Status_Approved;
			}

            _unitOfWork.OrderHeader.Add(ShoppingCartVM.OrderHeader);
            _unitOfWork.Save();

            foreach (var cart in ShoppingCartVM.ShoppingCartList)
            {
                OrderDetail order = new()
                {
                    OrderHeaderId = ShoppingCartVM.OrderHeader.Id,
                    ProductId = cart.ProductId,
                    Price = cart.Price,
                    Count = cart.Count
                };
                _unitOfWork.OrderDetail.Add(order);
                _unitOfWork.Save();
            }
			if (applicationUser.CompanyId.GetValueOrDefault() == 0)
			{
                // it is a regular user so we need to make him pay
                //Stripe Logic
                string domain = "https://localhost:7261/";
                var options = new SessionCreateOptions
                {
                    SuccessUrl = domain+$"Customer/ShoppingCart/OrderConfirmation?id={ShoppingCartVM.OrderHeader.Id}",
                    CancelUrl = domain+"Customer/ShoppingCart/Index",
                    LineItems = new List<SessionLineItemOptions>(),
                    Mode = "payment",
                };

                foreach (var item in ShoppingCartVM.ShoppingCartList)
                {
                    var sessionLineItem = new SessionLineItemOptions
                    {
                        PriceData = new SessionLineItemPriceDataOptions
                        {
                            UnitAmount = (long)(item.Price * 100),
                            Currency = "usd",
                            ProductData = new SessionLineItemPriceDataProductDataOptions
                            {
                                Name = item.product.Title
                            }
                        },
                        Quantity = item.Count
                    };
                    options.LineItems.Add(sessionLineItem);
                }

                var service = new SessionService();
                Session session = service.Create(options);

                _unitOfWork.OrderHeader.UpdateStripePaymentID(ShoppingCartVM.OrderHeader.Id, session.Id, session.PaymentIntentId);
                _unitOfWork.Save();

                Response.Headers.Add("Location", session.Url);
                return new StatusCodeResult(303);


			}
			return RedirectToAction(nameof(OrderConfirmation),new {id = ShoppingCartVM.OrderHeader.Id});
		}



        public IActionResult OrderConfirmation(int id)
        {
            OrderHeader order = _unitOfWork.OrderHeader.GetOne(o => o.Id == id, includeproperties: "ApplicationUser");
            if(order.PaymentStatus != SD.PaymentStatusDelayedPayment)
            {
                //this is a payment for regular customer
                var service = new SessionService();
                Session session = service.Get(order.SessionId);
                
                if(session.PaymentStatus.ToLower() == "paid")
                {
                    _unitOfWork.OrderHeader.UpdateStripePaymentID(id, session.Id, session.PaymentIntentId);
                    _unitOfWork.OrderHeader.UpdateStatus(id, SD.Status_Approved, SD.PaymentStatusApproved);
                    _unitOfWork.Save();

                }
                HttpContext.Session.Clear();

            }
            List<ShoppingCart> shoppingCarts = _unitOfWork.ShoppingCart
                .GetAll(c => c.ApplicationUserId == order.ApplicationUserId).ToList();
            _unitOfWork.ShoppingCart.RemoveRange(shoppingCarts);
            _unitOfWork.Save();

            return View(id);
        }



		public IActionResult Plus(int id)
        {
            ShoppingCart cart = _unitOfWork.ShoppingCart.GetOne(c => c.ShoppingCartId == id);

            cart.Count += 1;
            _unitOfWork.ShoppingCart.Update(cart);
            _unitOfWork.Save();

            return RedirectToAction(nameof(Index));
        }



        public IActionResult Minus(int id)
        {
            ShoppingCart cart = _unitOfWork.ShoppingCart.GetOne(c => c.ShoppingCartId == id);

            if(cart.Count == 1)
            {
                _unitOfWork.ShoppingCart.Remove(cart);
                HttpContext.Session.SetInt32(SD.SessionCart, _unitOfWork.ShoppingCart.GetAll(s => s.ApplicationUserId == cart.ApplicationUserId).Count() - 1);
            }
            else
            {
                cart.Count -= 1;
                _unitOfWork.ShoppingCart.Update(cart);
            }
            _unitOfWork.Save();

            return RedirectToAction(nameof(Index));
        }



        public IActionResult Remove(int id)
        {
            ShoppingCart cart = _unitOfWork.ShoppingCart.GetOne(c => c.ShoppingCartId == id);
            _unitOfWork.ShoppingCart.Remove(cart);
            HttpContext.Session.SetInt32(SD.SessionCart, _unitOfWork.ShoppingCart.GetAll(s => s.ApplicationUserId == cart.ApplicationUserId).Count() - 1);
            _unitOfWork.Save();

            return RedirectToAction(nameof(Index));
        }



        private double GetPriceBasedOnQuantity(ShoppingCart shoppingCart)
        {
            if (shoppingCart.Count <= 50)
                return shoppingCart.product.Price;
            else if (shoppingCart.Count >= 100)
                return shoppingCart.product.Price50;
            else
                return shoppingCart.product.Price100;
        }
    }
}
