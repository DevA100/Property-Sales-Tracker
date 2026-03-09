using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using PropertySalesTracker.Data;
using PropertySalesTracker.Models;
using PropertySalesTracker.Services.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PropertySalesTracker.Services
{
    public class ReminderService : IReminderService
    {
        private readonly AppDbContext _db;
        private readonly IEmailService _emailService;
        private readonly ISmsService _smsService;
        private readonly IConfiguration _config;
        private readonly UserManager<ApplicationUser> _userManager;
        public ReminderService(
            AppDbContext db,
            IEmailService emailService,
            ISmsService smsService,
            IConfiguration config,
           UserManager<ApplicationUser> userManager )
        {
            _db = db;
            _emailService = emailService;
            _smsService = smsService;
            _config = config;
            _userManager = userManager;
        }

        public async Task SendRemindersAsync()
        {
            var today = DateTime.UtcNow.Date;

            var sales = await _db.PropertySales
                .Include(s => s.Customer)
                .Include(s => s.AccountOfficer)
                .Include(s => s.Property)
                .ToListAsync();

            var officerGroupedList = new Dictionary<string, List<PropertySale>>();

            foreach (var sale in sales)
            {
                if (SkipSubscriptionType(sale))
                    continue;

                var nextDue = CalculateNextDueDate(sale);
                if (!nextDue.HasValue)
                    continue;

                var daysLeft = (nextDue.Value.Date - today).TotalDays;

                if (daysLeft is 30 or 14 or 7 or 3)
                {
                    await SendReminderForSaleAsync(sale, nextDue.Value);

                    var officerId = sale.Customer?.AccountOfficerId; 
                    if (!string.IsNullOrEmpty(officerId))
                    {
                        if (!officerGroupedList.ContainsKey(officerId))
                            officerGroupedList[officerId] = new List<PropertySale>();

                        officerGroupedList[officerId].Add(sale);
                    }
                }
            }

            foreach (var group in officerGroupedList)
            {
                var officerId = group.Key;

                var officerUser = await _userManager.FindByIdAsync(officerId);
                string officerEmail = officerUser?.Email;

                if (string.IsNullOrEmpty(officerEmail))
                {
                    var officer = await _db.AccountOfficers
                        .AsNoTracking()
                        .FirstOrDefaultAsync(o => o.UserId == officerId); 

                    officerEmail = officer?.Email;
                }

                if (string.IsNullOrWhiteSpace(officerEmail))
                    continue;

                string body = "<h3>Customers With Upcoming Due Payments</h3><ul>";

                foreach (var s in group.Value)
                {
                    var nextDue = CalculateNextDueDate(s);
                    body += $"<li>{s.Customer.FirstName} {s.Customer.LastName} — {s.Property?.Name} — Due {nextDue:dd MMM yyyy}</li>";
                }

                body += "</ul>";

                await _emailService.SendEmailAsync(
                    officerEmail,
                    "Upcoming Customer Payment Deadlines",
                    body
                );
            }
        }


        public async Task SendSingleReminderAsync(int saleId)
        {
            var sale = await _db.PropertySales
                .Include(s => s.Customer)
                .Include(s => s.AccountOfficer)
                .Include(s => s.Property)
                .FirstOrDefaultAsync(s => s.Id == saleId);

            if (sale == null || SkipSubscriptionType(sale))
                return;

            var nextDue = CalculateNextDueDate(sale);
            if (!nextDue.HasValue)
                return;

            await SendReminderForSaleAsync(sale, nextDue.Value);
        }

        private bool SkipSubscriptionType(PropertySale sale)
        {
            return sale.SubscriptionPlan == SubscriptionPlan.Outright ||
                   sale.SubscriptionPlan == SubscriptionPlan.Mortgaged;
        }

        private DateTime? CalculateNextDueDate(PropertySale sale)
        {
            var start = sale.LastPaymentDate ?? sale.StartDate;

            return sale.SubscriptionPlan switch
            {
                SubscriptionPlan.Monthly => start.AddMonths(1),
                SubscriptionPlan.Quarterly => start.AddMonths(3),
                SubscriptionPlan.Yearly => start.AddYears(1),
                _ => null
            };
        }

        private async Task SendReminderForSaleAsync(PropertySale sale, DateTime dueDate)
        {
            var customer = sale.Customer;
            var property = sale.Property;

            if (customer == null)
                return;

            var paymentLink = $"{_config["Payment:BaseUrl"]}/pay?saleId={sale.Id}";

            var html = $@"
                <p>Hello {customer.FirstName},</p>
                <p>Your payment for <strong>{property?.Name}</strong> 
                is due on <strong>{dueDate:dd MMM yyyy}</strong>.</p>
                <p>Kindly contact your account officer and make your payment.</p>
                <p>Thank you,<br/>Property Sales ,<br/> estate Plc.</p>
            ";

            if (!string.IsNullOrEmpty(customer.Email))
                await _emailService.SendEmailAsync(customer.Email, "Payment Reminder", html);

            if (!customer.IsDiaspora && !string.IsNullOrEmpty(customer.Phone))
            {
                var sms = $"Hi {customer.FirstName}, your payment for {property?.Name} is due on {dueDate:dd MMM yyyy}.";
                await _smsService.SendSmsAsync(customer.Phone, sms);
            }

            if (string.IsNullOrEmpty(customer.AccountOfficerId))
                return;

            var officer = await _db.AccountOfficers
                .AsNoTracking()
                .FirstOrDefaultAsync(o => o.UserId == customer.AccountOfficerId);

            if (officer == null)
                return;

            string officerEmail = null;

            if (!string.IsNullOrEmpty(officer.UserId))
            {
                var officerUser = await _userManager.FindByIdAsync(officer.UserId);
                if (officerUser != null)
                    officerEmail = officerUser.Email;
            }

            officerEmail ??= officer.Email;

            if (string.IsNullOrWhiteSpace(officerEmail))
                return;

            var officerMsg =
                $"Customer {customer.FirstName} {customer.LastName} has a payment due on {dueDate:dd MMM yyyy} for {property?.Name}.";

            await _emailService.SendEmailAsync(
                officerEmail,
                "Customer Payment Due",
                officerMsg
            );

        }
    }
}
