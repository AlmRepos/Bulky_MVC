using Bulky.DataAccess.Data;
using Bulky.Models;
using Bulky.Utility;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bulky.DataAccess.DbInitializer
{
    public class DbInitializer : IDbInitializer
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly Context _db;

        public DbInitializer(UserManager<IdentityUser> userManager,RoleManager<IdentityRole> roleManager, Context db)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _db = db;
        }
        public void Initialize()
        {

            //Migration if they are not applied
            try
            {
                if(_db.Database.GetPendingMigrations().Count()> 0)
                {
                    _db.Database.Migrate();
                }
            }
            catch(Exception e)
            {

            }

            //Create Roles if they are not created
            if (!_roleManager.RoleExistsAsync(SD.Role_Employee).GetAwaiter().GetResult())
            {
                _roleManager.CreateAsync(new IdentityRole(SD.Role_Employee)).GetAwaiter().GetResult();
                _roleManager.CreateAsync(new IdentityRole(SD.Role_Customer)).GetAwaiter().GetResult();
                _roleManager.CreateAsync(new IdentityRole(SD.Role_Admin)).GetAwaiter().GetResult();
                _roleManager.CreateAsync(new IdentityRole(SD.Role_Company)).GetAwaiter().GetResult();



                //if Roles not Created , then we will create admin User As Well
                _userManager.CreateAsync(new ApplicationUser()
                {
                    UserName = "admin@gmail.com",
                    Email = "admin@gmail.com",
                    Name = "Mohammed Alm El-Dien",
                    PhoneNumber = "01010164171",
                    StreetAddress = "Ash shohadaa - Salah El-Dien ST.",
                    City = "Minufya",
                    State = "Egypt",
                    PostalCode = "12345"
                }, "Admin123*").GetAwaiter().GetResult();


                ApplicationUser user = _db.ApplicationUsers.FirstOrDefault(u => u.Email == "admin@gmail.com");
                _userManager.AddToRoleAsync(user, SD.Role_Admin).GetAwaiter().GetResult();
            }


            return;
        }
    }
}
