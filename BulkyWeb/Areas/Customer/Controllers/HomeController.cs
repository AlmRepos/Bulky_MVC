using Bulky.DataAccess.IRepository;
using Bulky.Models;
using Bulky.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Security.Claims;

namespace BulkyWeb.Areas.Customer.Controllers
{
    [Area("Customer")]
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IUnitOfWork _unitOfWork;

        public HomeController(ILogger<HomeController> logger,IUnitOfWork unitOfWork)
        {
            _logger = logger;
            _unitOfWork = unitOfWork;
        }

        public IActionResult Index()
        {
            //var claimsIdentity = (ClaimsIdentity)User.Identity;
            //var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);

            //if (claim != null)
            //{
            //    HttpContext.Session.SetInt32(SD.SessionCart, _unitOfWork.ShoppingCart.GetAll(s => s.ApplicationUserId == claim.Value).Count());
            //}
            //else
            //{
            //    HttpContext.Session.Clear();
            //}
            IEnumerable<Product> products = _unitOfWork.Product.GetAll().ToList();

            return View(products);
        }

        public IActionResult Details(int id)
            {
            ShoppingCart shoppingCart = new()
            {
                product = _unitOfWork.Product.GetOne(p => p.Id == id, includeproperties: "category"),
                Count = 1,
                ProductId = id
            };
            return View(shoppingCart);
        }
        //public IActionResult Details(int productId)
        //{
        //    ShoppingCart cart = new()
        //    {
        //        product = _unitOfWork.Product.GetOne(u => u.Id == productId, includeproperties: "category"),
        //        Count = 1,
        //        ProductId = productId
        //    };
        //    return View(cart);
        //}
        [HttpPost]
        [Authorize]
        public IActionResult Details(ShoppingCart shoppingCart)
        {
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;
            shoppingCart.ApplicationUserId = userId;

            ShoppingCart fromdb = _unitOfWork.ShoppingCart.GetOne(s => s.ApplicationUserId == userId &&
            s.ProductId == shoppingCart.ProductId);

            if (fromdb != null)
            {
                //shopping cart exist 
                fromdb.Count += shoppingCart.Count; // this line will update the object automatically
                _unitOfWork.ShoppingCart.Update(fromdb);
                _unitOfWork.Save();
            }
            else
            {
                // add cart record

                //shoppingCart.Id = 0;
                _unitOfWork.ShoppingCart.Add(shoppingCart);
                _unitOfWork.Save();
                HttpContext.Session.SetInt32(SD.SessionCart, _unitOfWork.ShoppingCart.GetAll(s => s.ApplicationUserId == userId).Count());
            }

            
            TempData["success"] = "Cart Updated Successfully";

            return RedirectToAction(nameof(Index));
        }
        //public IActionResult Details(ShoppingCart shoppingCart)
        //{

        //    var claimsIdentity = (ClaimsIdentity)User.Identity;
        //    var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;
        //    shoppingCart.ApplicationUserId = userId;

        //    _unitOfWork.ShoppingCart.Add(shoppingCart);
        //    _unitOfWork.Save();


        //    return RedirectToAction(nameof(Index));
        //}
        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}