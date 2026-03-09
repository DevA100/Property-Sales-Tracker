using Microsoft.EntityFrameworkCore;
using PropertySalesTracker.Data;
using PropertySalesTracker.Models;
using PropertySalesTracker.Services.Interface;
using System.Collections.Generic;
using System.Threading.Tasks;
using ClosedXML.Excel;

namespace PropertySalesTracker.Services
{
    public class PropertyService : IPropertyService
    {
        private readonly AppDbContext _context;

        public PropertyService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<Property>> GetAllAsync()
        {
            return await _context.Properties.ToListAsync();
        }

        public async Task<Property?> GetByIdAsync(int id)
        {
            return await _context.Properties.FindAsync(id);
        }

        public async Task CreateAsync(Property property)
        {
            _context.Properties.Add(property);
            await _context.SaveChangesAsync();
        }




        public async Task UpdateAsync(Property property)
        {
            _context.Properties.Update(property);
            await _context.SaveChangesAsync();
        }


        public async Task<int> ImportAsync(IFormFile file)
        {
            using var stream = file.OpenReadStream();
            using var workbook = new XLWorkbook(stream);
            var sheet = workbook.Worksheet(1);

            var rows = sheet.RowsUsed().Skip(1);

            var existingNames = await _context.Properties
                .Select(p => p.Name.ToLower())
                .ToListAsync();

            int added = 0;

            foreach (var row in rows)
            {
                try
                {
                    var name = row.Cell(1).GetString()?.Trim();

                    if (string.IsNullOrWhiteSpace(name))
                        continue;

                    if (existingNames.Contains(name.ToLower()))
                        continue;

                    var propertyTypeRaw = row.Cell(6).GetString()?.Trim();

                    string propertyType = "Flat";
                    if (!string.IsNullOrWhiteSpace(propertyTypeRaw) &&
                        propertyTypeRaw.Equals("Shop", StringComparison.OrdinalIgnoreCase))
                    {
                        propertyType = "Shop";
                    }

                    var sellingPriceNullable = TryGetDecimal(row.Cell(10));

                    if (!sellingPriceNullable.HasValue)
                        continue;

                    var property = new Property
                    {
                        Name = name,
                        Description = string.IsNullOrWhiteSpace(row.Cell(2).GetString())
                            ? null
                            : row.Cell(2).GetString().Trim(),

                        Location = string.IsNullOrWhiteSpace(row.Cell(3).GetString())
                            ? null
                            : row.Cell(3).GetString().Trim(),

                        Size = string.IsNullOrWhiteSpace(row.Cell(4).GetString())
                            ? null
                            : row.Cell(4).GetString().Trim(),

                        Unit = TryGetInt(row.Cell(5)),
                        PropertyType = propertyType,
                        SellingPrice = sellingPriceNullable.Value,
                        FormFee = TryGetDecimal(row.Cell(9))
                    };

                    if (propertyType == "Flat")
                    {
                        property.CostPerUnit = TryGetDecimal(row.Cell(7));
                        property.CostPerSquare = null;
                    }
                    else
                    {
                        property.CostPerSquare = string.IsNullOrWhiteSpace(row.Cell(8).GetString())
                            ? null
                            : row.Cell(8).GetString().Trim();

                        property.CostPerUnit = null;
                    }

                    _context.Properties.Add(property);
                    added++;
                }
                catch (Exception ex)
                {
                    throw new Exception($"Import error at row {row.RowNumber()}: {ex.Message}");
                }
            }

            await _context.SaveChangesAsync();

            if (added == 0)
                throw new Exception("No properties were imported. Check your Excel format.");

            return added;
        }




        private decimal? TryGetDecimal(IXLCell cell)
        {
            if (cell.IsEmpty()) return null;

            var raw = cell.GetString()
                          .Replace(",", "")
                          .Replace("₦", "")
                          .Trim();

            if (decimal.TryParse(raw, out var value))
                return value;

            if (cell.TryGetValue<decimal>(out value))
                return value;

            return null;
        }

        private int? TryGetInt(IXLCell cell)
        {
            if (cell.IsEmpty()) return null;

            var raw = cell.GetString().Trim();

            if (int.TryParse(raw, out var value))
                return value;

            if (cell.TryGetValue<int>(out value))
                return value;

            return null;
        }





        public async Task DeleteAsync(int id)
        {
            var property = await _context.Properties.FindAsync(id);
            if (property != null)
            {
                _context.Properties.Remove(property);
                await _context.SaveChangesAsync();
            }
        }
    }
}
