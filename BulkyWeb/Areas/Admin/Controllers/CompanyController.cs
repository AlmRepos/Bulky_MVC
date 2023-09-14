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

    public class CompanyController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;

        public CompanyController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public IActionResult Index()
        {
            return View(_unitOfWork.Company.GetAll().ToList());
        }

        public IActionResult Upsert(int? id)
        {
            Company company = new Company();
            if (id == null || id == 0)
            {
                return View(company);
            }
            else
            {
                company = _unitOfWork.Company.GetOne(p => p.Id == id);
                return View(company);
            }
        }
        [HttpPost]
        public IActionResult Upsert(Company company)
        {
            if (ModelState.IsValid)
            {
                if(company.Id == 0)
                {
                    _unitOfWork.Company.Add(company);
                    TempData["success"] = "Company Added Successfully";
                }
                else
                {
                    _unitOfWork.Company.Update(company);
                    TempData["success"] = "Company Updated Successfully";
                }
                _unitOfWork.Save();
                return RedirectToAction("Index");
            }
                return View(company);
        }

        [HttpPost]
        public IActionResult Delete(Company company)
        {
            _unitOfWork.Company.Remove(company);
            _unitOfWork.Save();
            TempData["success"] = "Company Deleted Successfully";
            return RedirectToAction("Index");
        }

        #region Api Calls
        [HttpGet]
        public IActionResult GetALl()
        {
            return Json(new { data = _unitOfWork.Company.GetAll().ToList() });
        }
        [HttpDelete]
        public IActionResult Delete(int? id)
        {
            Company company = _unitOfWork.Company.GetOne(p => p.Id == id);

            if (company != null)
            {
                _unitOfWork.Company.Remove(company);
                _unitOfWork.Save();
                return Json(new { success = true, message = "Company deleted Successfully" });
            }
            else
            {
                return Json(new { success = false, message = "Error while deleting" });
            }

        }
        #endregion


    }
}
