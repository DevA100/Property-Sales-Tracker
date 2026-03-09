using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using PropertySalesTracker.Data;
using PropertySalesTracker.Models;
using PropertySalesTracker.Models.DTOs;

using PropertySalesTracker.Services.Interface;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace PropertySalesTracker.Controllers
{
    [Authorize]
    public class CustomerController : Controller
    {
        private readonly ICustomerService _customerService;
        private readonly AppDbContext _context;
        private readonly ILogger<CustomerController> _logger;
        private readonly IEmailService _emailService;

        public CustomerController(ICustomerService customerService, IEmailService emailService, AppDbContext context, ILogger<CustomerController> logger)
        {
            _customerService = customerService;
            _context = context;
            _logger = logger;
            _emailService = emailService;
        }

        public async Task<IActionResult> Index(string? search)
        {
            var customers = await _customerService.GetAllAsync();

            if (!string.IsNullOrWhiteSpace(search))
            {
                customers = customers.Where(c =>
                    (c.FirstName + " " + c.LastName).Contains(search, StringComparison.OrdinalIgnoreCase) ||
                    (c.Email ?? "").Contains(search, StringComparison.OrdinalIgnoreCase) ||
                    (c.Branch ?? "").Contains(search, StringComparison.OrdinalIgnoreCase)
                ).ToList();
            }

            return View(customers);
        }

        [HttpGet]
        public IActionResult Create()
        {
            var officers = _context.AccountOfficers.ToList();

            foreach (var o in officers)
            {
                if (string.IsNullOrWhiteSpace(o.Name))
                    o.Name = "(No Name)";
            }

            ViewBag.AccountOfficers = new SelectList(officers, "UserId", "Name");

            ViewBag.Properties = _context.Properties.ToList();

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CustomerCreateDto dto)
        {
            if (ModelState.IsValid)
            {
                var customer = await _customerService.CreateAsync(dto);

                try
                {
                    string fullName = $"{dto.FirstName} {dto.LastName}";
                    string subject = "Welcome to example Estate";
                    string body = $@"
            <p>Dear {fullName},</p>
            <p>Your registration with <strong> example  Estate</strong> was successful.</p>
            <p>Thank you for choosing  example   Plc.</p>
            <p>Warm regards,<br/>  </p>";

                    if (!string.IsNullOrWhiteSpace(dto.Email))
                        await _emailService.SendEmailAsync(dto.Email, subject, body);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"⚠️ Email sending failed: {ex.Message}");
                    TempData["Warning"] = "Customer created, but email could not be sent.";
                }

                TempData["Success"] = "Customer and property sale created successfully.";
                return RedirectToAction(nameof(Index));
            }

            var officers = _context.AccountOfficers.ToList();
            foreach (var o in officers)
            {
                if (string.IsNullOrWhiteSpace(o.Name))
                    o.Name = "(No Name)";
            }
            ViewBag.AccountOfficers = new SelectList(officers, "UserId", "Name", dto.AccountOfficerId);

            ViewBag.Properties = _context.Properties.ToList();

            return View(dto);
        }


        [HttpGet]
       public async Task<IActionResult> Edit(int id)
{
    var customer = await _customerService.GetByIdAsync(id);
    if (customer == null) return NotFound();

    var sale = customer.PropertySales?.FirstOrDefault();

    var dto = new CustomerUpdateDto
    {
        Id = customer.Id,
        FirstName = customer.FirstName,
        LastName = customer.LastName,
        Email = customer.Email,
        Phone = customer.Phone,
        Country = customer.Country,
        IsDiaspora = customer.IsDiaspora,
        AccountOfficerId = customer.AccountOfficerId,

        Branch = customer.Branch,
        PropertyId = sale?.PropertyId,
        SubscriptionPlan = sale?.SubscriptionPlan ?? SubscriptionPlan.Monthly,
        EquityDeposit = sale?.EquityDeposit,
        Balance = sale?.Balance,
        Month = sale?.Month,
        NextDueDate = sale?.NextDueDate,
        Status = sale?.Status,
        Unit = sale?.Unit ?? 0,
        Size = sale?.Size,
        FormFee = sale?.FormFee,
    };

    ViewBag.AccountOfficers = new SelectList(_context.AccountOfficers, "UserId", "Name", dto.AccountOfficerId);

    var properties = _context.Properties.ToList();

    ViewBag.Properties = new SelectList(properties, "Id", "Name", dto.PropertyId);

    ViewBag.PropertyPrices = properties.ToDictionary(
        p => p.Id,
p => p.PropertyType == "Flat"
     ? (p.CostPerUnit ?? 0m)
     : (decimal.TryParse(p.CostPerSquare, out var sq) ? sq : 0m)

    );

    return PartialView("_EditPartial", dto);
}


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, CustomerUpdateDto dto)
        {
            if (id != dto.Id) return NotFound();

            if (ModelState.IsValid)
            {
                await _customerService.UpdateAsync(id, dto);
                TempData["Success"] = "Customer and property sale updated successfully.";

                return RedirectToAction(nameof(Index));
            }

            ViewBag.AccountOfficers = new SelectList(_context.AccountOfficers, "UserId", "Name", dto.AccountOfficerId);
            ViewBag.Properties = _context.Properties.ToList(); 
            ViewBag.PropertyPrices = _context.Properties.ToDictionary(
                p => p.Id,
               p => p.PropertyType == "Flat"
     ? (p.CostPerUnit?.ToString("N0") ?? "0")
     : (string.IsNullOrWhiteSpace(p.CostPerSquare) ? "0" : p.CostPerSquare)

            );

            return PartialView("_EditPartial", dto);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Import(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                TempData["Error"] = "Please select a valid Excel file.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                await _customerService.ImportAsync(file);
                TempData["Success"] = "Customers imported successfully.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
                return RedirectToAction(nameof(Index));
            }


            return RedirectToAction(nameof(Index));
        }


        [HttpGet]
public async Task<IActionResult> ExportCustomers()
{
    try
    {
        var fileBytes = await _customerService.ExportCustomersAsync();

        return File(
            fileBytes,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"Customers_Report_{DateTime.Now:yyyyMMddHHmmss}.xlsx"
        );
    }
    catch
    {
        TempData["Error"] = "Failed to export customer report.";
        return RedirectToAction("Index");
    }
}

        [HttpGet]
        public async Task<IActionResult> DownloadImportTemplate()
        {
            var fileBytes = await _customerService.DownloadImportTemplateAsync();

            return File(
                fileBytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "Customer_Import_Template.xlsx"
            );
        }



        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            await _customerService.DeleteAsync(id);
            TempData["Success"] = "Customer deleted successfully.";
            return RedirectToAction(nameof(Index));
        }
    }
}
