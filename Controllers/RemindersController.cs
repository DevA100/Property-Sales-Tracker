using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PropertySalesTracker.Services.Interface;
using PropertySalesTracker.Data;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using PropertySalesTracker.Models;
using Microsoft.AspNetCore.Identity;

namespace PropertySalesTracker.Controllers
{
    [Authorize(Roles = "Admin,SuperAdmin")] 
    public class RemindersController : Controller
    {
        private readonly AppDbContext _db;
        private readonly IReminderService _reminderService;
        private readonly UserManager<ApplicationUser> _userManager;


        public RemindersController(AppDbContext db, IReminderService reminderService, UserManager<ApplicationUser> userManager)
        {
            _db = db;
            _reminderService = reminderService;
            _userManager = userManager;
        }

        public async Task<IActionResult> UpcomingPayments()
        {
            var today = DateTime.UtcNow.Date;

            var sales = await _db.PropertySales
                .Include(s => s.Customer)
                .Include(s => s.Property)
                .ToListAsync();

            var officerUsers = await _userManager.GetUsersInRoleAsync("AccountOfficer");

            var officers = officerUsers
                .ToDictionary(u => u.Id, u => u.FullName);


            var model = sales
                .Where(s =>
                    s.SubscriptionPlan != SubscriptionPlan.Outright &&
                    s.SubscriptionPlan != SubscriptionPlan.Mortgaged &&
                    s.Customer != null)
                .Select(s =>
                {
                    var last = s.LastPaymentDate ?? s.StartDate;

                    DateTime? nextDue = s.SubscriptionPlan switch
                    {
                        SubscriptionPlan.Monthly => last.AddMonths(1),
                        SubscriptionPlan.Quarterly => last.AddMonths(3),
                        SubscriptionPlan.Yearly => last.AddYears(1),
                        _ => null
                    };

                    if (!nextDue.HasValue)
                        return null;

                    var daysLeft = (nextDue.Value.Date - today).TotalDays;

                    string officerName = "—";

                    if (!string.IsNullOrEmpty(s.Customer?.AccountOfficerId) &&
                        officers.TryGetValue(s.Customer.AccountOfficerId, out var name))
                    {
                        officerName = name;
                    }


                    return new DueCustomerDto
                    {
                        SaleId = s.Id,
                        CustomerName = s.Customer.FirstName + " " + s.Customer.LastName,
                        PropertyName = s.Property?.Name,
                        Email = s.Customer.Email,
                        Phone = s.Customer.Phone,
                        IsDiaspora = s.Customer.IsDiaspora,
                        OfficerName = officerName,   
                        DueDate = nextDue.Value,
                        DaysLeft = (int)daysLeft
                    };
                })
                .Where(x => x != null)
                .OrderBy(x => x.DaysLeft)
                .ToList();

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Send(int id)
        {
            try
            {
                await _reminderService.SendSingleReminderAsync(id);
                TempData["Success"] = "Reminder sent successfully!";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Failed to send reminder: {ex.Message}";
            }

            return RedirectToAction("UpcomingPayments");
        }


        [HttpPost]
        public async Task<IActionResult> SendAll()
        {
            try
            {
                await _reminderService.SendRemindersAsync();
                TempData["Success"] = "Bulk reminders sent!";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Bulk reminder failed: {ex.Message}";
            }

            return RedirectToAction("UpcomingPayments");
        }

    }
}
