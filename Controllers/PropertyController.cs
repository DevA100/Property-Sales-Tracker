using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PropertySalesTracker.Models;
using PropertySalesTracker.Services.Interface;
using System.Threading.Tasks;

namespace PropertySalesTracker.Controllers
{
    public class PropertyController : Controller
    {
        private readonly IPropertyService _propertyService;

        public PropertyController(IPropertyService propertyService)
        {
            _propertyService = propertyService;
        }

        public async Task<IActionResult> Index()
        {
            var properties = await _propertyService.GetAllAsync();
            return View(properties);
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        [Authorize(Roles = "SuperAdmin,Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Property property)
        {
            
            if (property.PropertyType == "Flat")
            {
                property.CostPerSquare = null; 
            }
            else if (property.PropertyType == "Shop")
            {
                property.CostPerUnit = null;
            }

            await _propertyService.CreateAsync(property);
            TempData["Success"] = "Property created successfully.";
            return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles = "SuperAdmin,Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Property property)
        {
            if (!ModelState.IsValid)
            {
                TempData["Error"] = "Invalid form data.";
                return RedirectToAction(nameof(Index));
            }

            if (property.PropertyType == "Flat")
            {
                property.CostPerSquare = null;
            }
            else if (property.PropertyType == "Shop")
            {
                property.CostPerUnit = null;
            }

            await _propertyService.UpdateAsync(property);
            TempData["Success"] = "Property updated successfully.";
            return RedirectToAction(nameof(Index));
        }


        [Authorize(Roles = "SuperAdmin,Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Import(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                TempData["Error"] = "Please select a valid Excel file.";
                return RedirectToAction(nameof(Index));
            }

            await _propertyService.ImportAsync(file);

            TempData["Success"] = "Properties imported successfully.";
            return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles = "SuperAdmin,Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]

        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            await _propertyService.DeleteAsync(id);
            TempData["Success"] = "Property deleted successfully.";
            return RedirectToAction(nameof(Index));
        }



        [HttpGet]
        public IActionResult DownloadTemplate()
        {
            using var workbook = new XLWorkbook();
            var sheet = workbook.Worksheets.Add("Properties");

            sheet.Cell(1, 1).Value = "Name";
            sheet.Cell(1, 2).Value = "Description";
            sheet.Cell(1, 3).Value = "Location";
            sheet.Cell(1, 4).Value = "Size";
            sheet.Cell(1, 5).Value = "Unit";
            sheet.Cell(1, 6).Value = "PropertyType (Flat/Shop)";
            sheet.Cell(1, 7).Value = "CostPerUnit";
            sheet.Cell(1, 8).Value = "CostPerSquare (Text)";
            sheet.Cell(1, 9).Value = "FormFee";
            sheet.Cell(1, 10).Value = "SellingPrice";

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            stream.Position = 0;

            return File(
                stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "Property_Import_Template.xlsx"
            );
        }


    }
}
