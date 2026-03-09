using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using PropertySalesTracker.Models;
using PropertySalesTracker.Services.Interface;
using PropertySalesTracker.ViewModels;
using System.Linq;
using System.Threading.Tasks;
using PropertySalesTracker.Dtos;

namespace PropertySalesTracker.Controllers
{
    [Authorize]
    public class DashboardController : Controller
    {
        private readonly ICustomerService _customers;
        private readonly IPropertySaleService _sales;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IPropertyService _properties;


        public DashboardController(
            ICustomerService customers,
            IPropertySaleService sales,
             IPropertyService properties,
            UserManager<ApplicationUser> userManager)
        {
            _customers = customers;
            _sales = sales;
            _properties = properties;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            var roles = await _userManager.GetRolesAsync(user);
            bool isOfficer = roles.Contains("AccountOfficer");

            var allCustomers = await _customers.GetAllAsync();
            var allSales = await _sales.GetAllAsync();
            var allProperties = await _properties.GetAllAsync();

            if (isOfficer)
            {
                allCustomers = allCustomers
                    .Where(c => c.AccountOfficerId == user.Id)
                    .ToList();

                allSales = allSales
                    .Where(s => s.Customer != null && s.Customer.AccountOfficerId == user.Id)
                    .ToList();

                allProperties = allProperties
                    .Where(p => p.PropertySales.Any(ps => ps.Customer != null && ps.Customer.AccountOfficerId == user.Id))
                    .ToList();
            }


            var officers = await _userManager.GetUsersInRoleAsync("AccountOfficer");

            var model = new DashboardViewModel
            {
                TotalCustomers = allCustomers.Count(),
                TotalProperties = allProperties.Count(),
                SoldProperties = allProperties.Count(p => p.Status == PropertyStatus.Sold),
                AvailableProperties = allProperties.Count(p => p.Status == PropertyStatus.Available),
                ReservedProperties = allProperties.Count(p => p.Status == PropertyStatus.Reserved),
                TotalAccountOfficers = officers.Count(),
                RecentSales = allSales
                    .OrderByDescending(s => s.Id)
                    .Take(10)
                    .ToList()
            };

            return View(model);
        }

    }
}
