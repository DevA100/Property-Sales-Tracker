using ClosedXML.Excel;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using PropertySalesTracker.Data;
using PropertySalesTracker.Dtos;
using PropertySalesTracker.DTOs;
using PropertySalesTracker.Models;
using PropertySalesTracker.Services;
using PropertySalesTracker.Services.Interface;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;


public class PropertySaleController : Controller
{
    private readonly AppDbContext _context;
    private readonly IPropertySaleService _propertySaleService;

    public PropertySaleController(IPropertySaleService propertySaleService, AppDbContext context )
    {
        _context = context;
        _propertySaleService = propertySaleService;

    }


    public async Task<IActionResult> Index()
    {
        var sales = await _propertySaleService.GetAllAsync();
        return View(sales);
    }


    [HttpGet]
    public IActionResult Create()
    {
        LoadDropdowns();
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(PropertySaleCreateDto dto)
    {
        if (!ModelState.IsValid)
        {
            LoadDropdowns(dto);
            return View(dto);
        }

        await _propertySaleService.CreateAsync(dto);
        TempData["Success"] = "Property sale created successfully!";
        return RedirectToAction(nameof(Index));
    }

    
    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var sale = await _propertySaleService.GetByIdAsync(id);
        if (sale == null) return NotFound();

        var dto = new PropertySaleUpdateDto
        {
            Id = sale.Id,
            CustomerId = sale.CustomerId,
            AccountOfficerId = sale.AccountOfficerId,
            PropertyId = sale.PropertyId,
            Unit = sale.Unit,
            FormFee = sale.FormFee,
            EquityDeposit = sale.EquityDeposit,
            SellingPrice = sale.SellingPrice,
            Balance = sale.Balance,
            Description = sale.Description,
            Month = sale.Month,
            SubscriptionPlan = sale.SubscriptionPlan,
            Size = sale.Property?.Size
        };

        ViewBag.Customers = new SelectList(_context.Customers, "Id", "FirstName", sale.CustomerId);
        ViewBag.AccountOfficers = new SelectList(_context.AccountOfficers, "Id", "Name", sale.AccountOfficerId);
        ViewBag.Properties = new SelectList(_context.Properties, "Id", "Name", sale.PropertyId);

        return PartialView("_EditPartial", dto);
    }


    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update([FromForm] PropertySaleUpdateDto dto)
    {
        var updatedSale = await _propertySaleService.UpdateAsync(dto.Id, dto);
        if (updatedSale == null)
            return Json(new { success = false });

        return Json(new { success = true });
    }


    public async Task<IActionResult> Details(int id)
    {
        var sale = await _propertySaleService.GetByIdAsync(id);
        if (sale == null) return NotFound();
        return View(sale);
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _propertySaleService.DeleteAsync(id);
        if (!result)
            TempData["Error"] = "Unable to delete sale record.";
        else
            TempData["Success"] = "Sale deleted successfully.";

        return RedirectToAction(nameof(Index));
    }

    private void LoadDropdowns(PropertySaleCreateDto? dto = null)
    {
        ViewBag.Properties = new SelectList(_context.Properties, "Id", "Name", dto?.PropertyId);
        ViewBag.Customers = new SelectList(_context.Customers, "Id", "FullName", dto?.CustomerId);
        ViewBag.AccountOfficers = new SelectList(_context.AccountOfficers, "Id", "Name", dto?.AccountOfficerId);
    }





[HttpGet]
    public IActionResult Import()
    {
        return View();
    }


    [HttpPost]
    public async Task<IActionResult> Import(IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            TempData["Error"] = "Please upload a file.";
            return RedirectToAction(nameof(Index));
        }

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();

        using var stream = file.OpenReadStream();

        if (ext == ".csv")
        {
            await _propertySaleService.ImportFromCsvAsync(stream);
        }
        else if (ext == ".xlsx" || ext == ".xls")
        {
            await _propertySaleService.ImportFromExcelAsync(stream);
        }
        else
        {
            TempData["Error"] = "Unsupported file type.";
            return RedirectToAction(nameof(Index));
        }

        TempData["Success"] = "Import successful!";
        return RedirectToAction(nameof(Index));
    }



    private static decimal? TryParseDecimal(string input)
    {
        if (decimal.TryParse(input?.Replace(",", ""), out var result))
            return result;
        return null;
    }

    private static SubscriptionPlan ParsePlan(string plan)
    {
        if (string.IsNullOrEmpty(plan)) return SubscriptionPlan.Outright;
        plan = plan.ToLower();
        if (plan.Contains("month")) return SubscriptionPlan.Monthly;
        if (plan.Contains("quarter")) return SubscriptionPlan.Quarterly;
        if (plan.Contains("mort")) return SubscriptionPlan.Mortgaged;
        if (plan.Contains("year")) return SubscriptionPlan.Yearly;
        return SubscriptionPlan.Outright;
    }

    private static PropertyStatus ParseStatus(string status)
    {
        if (string.IsNullOrEmpty(status)) return PropertyStatus.Available;
        status = status.ToLower();
        if (status.Contains("sold")) return PropertyStatus.Sold;
        if (status.Contains("reserve")) return PropertyStatus.Reserved;
        return PropertyStatus.Available;
    }
}
