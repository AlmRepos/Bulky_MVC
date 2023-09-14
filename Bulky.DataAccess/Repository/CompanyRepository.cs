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
    public class CompanyRepository : Repository<Company>, ICompanyRepository
    {
        private readonly Context _db;

        public CompanyRepository(Context db) :base(db)
            {
            _db = db;
        }
        public void Update(Company company)
        {
            if(company != null)
            {
                Company? old = _db.Companies.FirstOrDefault(p=>p.Id == company.Id);
                if(old != null)
                {
                    old.Name = company.Name;
                    old.City = company.City;
                    old.StreetAddress = company.StreetAddress;
                    old.State = company.State;
                    old.PhoneNumber = company.PhoneNumber;
                    old.PostalCode = company.PostalCode;
                }
            }
        }
    }
}
