using Bulky.DataAccess.IRepository;
using Bulky.Models;
using Bulky.Models.ViewModels;
using Bulky.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace BulkyWeb.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SD.Role_Admin)]

    public class ProductController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public ProductController(IUnitOfWork unitOfWork,IWebHostEnvironment webHostEnvironment)
        {
            _unitOfWork = unitOfWork;
            _webHostEnvironment = webHostEnvironment;
        }

        public IActionResult Index()
        {
            return View(_unitOfWork.Product.GetAll(includeproperties: "category").ToList());
        }

        public IActionResult Upsert(int? id)
        {
            //ViewBag.CatList = CategoryList;
            //ViewData["CatList"] = CategoryList;
            ProductVM productVM = new ProductVM()
            {
                CategoryList = _unitOfWork.Category.GetAll().Select(s => new SelectListItem
                {
                    Text = s.Name,
                    Value = s.Id.ToString()
                }),
                product = new Product()
            };
            if (id == null || id == 0)
            {
                return View(productVM);
            }
            else
            {
                productVM.product = _unitOfWork.Product.GetOne(p => p.Id == id);
                return View(productVM);
            }
        }
        [HttpPost]
        public IActionResult Upsert(ProductVM VM, IFormFile? file)
        {
            if (ModelState.IsValid)
            {
                string wwwrootpath = _webHostEnvironment.WebRootPath;
                if(file != null)
                {
                    string fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);

                    string filePath = Path.Combine(wwwrootpath + @"\Images\Product");
                    if(!string.IsNullOrEmpty(VM.product.ImageUrl))
                    {
                        string old = Path.Combine(wwwrootpath, VM.product.ImageUrl.TrimStart('\\'));
                        if(System.IO.File.Exists(old))
                        {
                            System.IO.File.Delete(old);
                        }

                    }
                    using (var filestream = new FileStream(Path.Combine(filePath, fileName), FileMode.Create))
                    {
                        file.CopyTo(filestream);
                    }
                    VM.product.ImageUrl = @"\Images\Product\" + fileName;
                }
                if (VM.product.Id == 0)
                {
                    _unitOfWork.Product.Add(VM.product);
                    TempData["success"] = "Product Created Successfully";
                }
                else
                {
                    _unitOfWork.Product.Update(VM.product);
                    TempData["success"] = "Product Updated Successfully";
                }
                _unitOfWork.Save();
                return RedirectToAction("Index");
            }
            else
            {
                VM.CategoryList = _unitOfWork.Category.GetAll().Select(s => new SelectListItem
                {
                    Text = s.Name,
                    Value = s.Id.ToString()
                });
                return View();

            }
        }

        [HttpPost]
        public IActionResult Delete(Product product)
        {
            _unitOfWork.Product.Remove(product);
            _unitOfWork.Save();
            TempData["success"] = "Product Deleted Successfully";
            return RedirectToAction("Index");
        }

        #region Api Calls
        [HttpGet]
        public IActionResult GetALl()
        {
            return Json(new { data = _unitOfWork.Product.GetAll(includeproperties: "category").ToList() });
        }
        [HttpDelete]
        public IActionResult Delete(int? id)
        {
            Product product = _unitOfWork.Product.GetOne(p => p.Id == id);

            if (product != null)
            {
                var image =
                Path.Combine(_webHostEnvironment.WebRootPath,
                    product.ImageUrl.TrimStart('\\'));

                if (System.IO.File.Exists(image))
                {
                    System.IO.File.Delete(image);
                }
                _unitOfWork.Product.Remove(product);
                _unitOfWork.Save();
                return Json(new { success = true, message = "Product deleted Successfully" });

            }
            else
            {
                return Json(new { success = false, message = "Error while deleting" });
            }

        }
        #endregion


    }
}
