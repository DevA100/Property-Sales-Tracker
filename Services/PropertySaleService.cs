using ClosedXML.Excel;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PropertySalesTracker.Data;
using PropertySalesTracker.Dtos;
using PropertySalesTracker.DTOs;
using PropertySalesTracker.Models;
using PropertySalesTracker.Services.Interface;
using System.Formats.Asn1;
using System.Globalization;

namespace PropertySalesTracker.Services
{
    public class PropertySaleService : IPropertySaleService
    {
        private readonly AppDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        public PropertySaleService(AppDbContext db, UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager)
        {
            _db = db;
            _userManager = userManager;
            _roleManager = roleManager;
        }


        public async Task<PropertySale> CreateAsync(PropertySaleCreateDto dto)
        {
            var sale = new PropertySale
            {
                Month = dto.Month,
                CustomerId = dto.CustomerId,
                AccountOfficerId = dto.AccountOfficerId,
                PropertyId = dto.PropertyId,
                Unit = dto.Unit,
                FormFee = dto.FormFee,
                EquityDeposit = dto.EquityDeposit,
                SellingPrice = dto.SellingPrice,
                Balance = dto.Balance,
                Description = dto.Description,
                Status = PropertyStatus.Reserved,
                StartDate = DateTime.UtcNow
            };

            if (dto.IsMonthly) sale.SubscriptionPlan = SubscriptionPlan.Monthly;
            else if (dto.IsQuarterly) sale.SubscriptionPlan = SubscriptionPlan.Quarterly;
            else if (dto.IsMortgage) sale.SubscriptionPlan = SubscriptionPlan.Mortgaged;
            else if (dto.IsOutright) sale.SubscriptionPlan = SubscriptionPlan.Outright;
            else sale.SubscriptionPlan = SubscriptionPlan.Outright;

            _db.PropertySales.Add(sale);
            await _db.SaveChangesAsync();
            return sale;
        }

        public async Task<PropertySale?> UpdateAsync(int id, PropertySaleUpdateDto dto)
        {
            var sale = await _db.PropertySales.FindAsync(id);
            if (sale == null) return null;



            sale.Month = dto.Month ?? "0";

            sale.CustomerId = dto.CustomerId;
            sale.AccountOfficerId = dto.AccountOfficerId;
            sale.PropertyId = dto.PropertyId ?? 0;
            sale.Unit = dto.Unit ?? 0;
            sale.FormFee = dto.FormFee;
            sale.EquityDeposit = dto.EquityDeposit;
            sale.SellingPrice = dto.SellingPrice;
            sale.Balance = dto.Balance;
            sale.Description = dto.Description;
            sale.UpdatedAt = DateTime.UtcNow;

            if (dto.IsMonthly) sale.SubscriptionPlan = SubscriptionPlan.Monthly;
            else if (dto.IsQuarterly) sale.SubscriptionPlan = SubscriptionPlan.Quarterly;
            else if (dto.IsMortgage) sale.SubscriptionPlan = SubscriptionPlan.Mortgaged;
            else if (dto.IsOutright) sale.SubscriptionPlan = SubscriptionPlan.Outright;
            else if (dto.IsMilestone) sale.SubscriptionPlan = SubscriptionPlan.Yearly;

            _db.PropertySales.Update(sale);
            await _db.SaveChangesAsync();
            return sale;
        }


        public async Task<List<PropertySale>> GetAllAsync() =>
            await _db.PropertySales
    .Include(s => s.Customer)
    .Include(s => s.Property)
    .Include(s => s.AccountOfficer)
    .ToListAsync();

        public async Task<PropertySale?> GetByIdAsync(int id) =>
            await _db.PropertySales
                .Include(s => s.Customer)
                .Include(s => s.AccountOfficer)
                .Include(s => s.Property)
                .FirstOrDefaultAsync(s => s.Id == id);

        public async Task<bool> DeleteAsync(int id)
        {
            var sale = await _db.PropertySales.FindAsync(id);
            if (sale == null)
                return false;

            _db.PropertySales.Remove(sale);
            await _db.SaveChangesAsync();
            return true;
        }

        public async Task ImportFromCsvAsync(Stream csvStream)
        {
            using var reader = new StreamReader(csvStream);
            using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                MissingFieldFound = null,
                HeaderValidated = null,
                BadDataFound = null,
                TrimOptions = TrimOptions.Trim
            });

            var records = csv.GetRecords<dynamic>().ToList();

            foreach (var record in records)
            {
                var row = ((IDictionary<string, object>)record)
                    .ToDictionary(k => k.Key.ToUpper(), v => v.Value?.ToString());

                await ProcessRowAsync(row);
            }

            await _db.SaveChangesAsync();
        }

        public async Task ImportFromExcelAsync(Stream excelStream)
        {
            using var workbook = new XLWorkbook(excelStream);
            var sheet = workbook.Worksheet(1);
            var rows = sheet.RangeUsed().RowsUsed().Skip(1);

            foreach (var row in rows)
            {
                try
                {
                    var data = new Dictionary<string, string?>
                    {
                        ["MONTH"] = row.Cell(1).GetString(),
                        ["ACCOUNT OFFICER"] = row.Cell(2).GetString(),
                        ["CUSTOMER"] = row.Cell(3).GetString(),
                        ["PHONE"] = row.Cell(4).GetString(),
                        ["EMAIL"] = row.Cell(5).GetString(),
                        ["BRANCH"] = row.Cell(6).GetString(),
                        ["PROPERTY"] = row.Cell(7).GetString(),
                        ["SIZE"] = row.Cell(8).GetString(),
                        ["DESCRIPTION"] = row.Cell(10).GetString(),
                        ["UNIT"] = row.Cell(11).GetString(),
                        ["SELLING PRICE"] = row.Cell(14).GetString(),
                        ["DEPOSIT"] = row.Cell(13).GetString(),
                        ["OUTSTANDING"] = row.Cell(15).GetString(),
                        ["PLAN"] = row.Cell(16).GetString(),
                        ["STATUS"] = row.Cell(17).GetString()
                    };

                    await ProcessRowAsync(data);
                }
                catch
                {
                }
            }
        }

        private async Task ProcessRowAsync(Dictionary<string, string?> row)
        {
            string Get(string key) => row.TryGetValue(key, out var v) ? v?.Trim() ?? "" : "";

            var propertyName = Get("PROPERTY");
            if (string.IsNullOrWhiteSpace(propertyName))
                return;

            var officerName = Get("ACCOUNT OFFICER");
            var customerName = Get("CUSTOMER");
            var phone = Get("PHONE");
            var branch = Get("BRANCH");
            var size = Get("SIZE");
            var description = Get("DESCRIPTION");
            var month = Get("MONTH");

            var rawEmail = Get("EMAIL");
            var email = string.IsNullOrWhiteSpace(rawEmail)
                ? $"{Guid.NewGuid()}@temp.local"
                : rawEmail.ToLower();

            int unit = int.TryParse(Get("UNIT"), out var u) ? u : 1;

            decimal? sellingPrice = TryParseDecimal(Get("SELLING PRICE"));
            decimal? equity = TryParseDecimal(Get("DEPOSIT"));
            decimal? balance = TryParseDecimal(Get("OUTSTANDING"));

            AccountOfficer? officer = null;
            string? officerId = null; 

            if (!string.IsNullOrWhiteSpace(officerName))
            {
                var officerEmail = $"{officerName.Replace(" ", "").ToLower()}@temp.local";

                await GetOrCreateUserAsync(officerName, officerEmail, branch);

                officer = await _db.AccountOfficers
                    .FirstOrDefaultAsync(o => o.Email == officerEmail);

                if (officer == null)
                {
                    officer = new AccountOfficer
                    {
                        Name = officerName,
                        Email = officerEmail,
                        Phone = phone ?? "N/A",
                        Department = branch
                    };

                    _db.AccountOfficers.Add(officer);
                    await _db.SaveChangesAsync();
                }

                officerId = officer.UserId;

            }

            var customer = await _db.Customers
                .FirstOrDefaultAsync(c => c.Email == email);

            if (customer == null)
            {
                var names = customerName.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);

                customer = new Customer
                {
                    FirstName = names.ElementAtOrDefault(0) ?? "Unknown",
                    LastName = names.ElementAtOrDefault(1) ?? "",
                    Email = email,
                    Phone = phone,
                    Branch = branch,
                    AccountOfficerId = officerId
                };

                _db.Customers.Add(customer);
                await _db.SaveChangesAsync();
            }

            var property = await _db.Properties
                .FirstOrDefaultAsync(p => p.Name == propertyName);

            if (property == null)
            {
                property = new Property
                {
                    Name = propertyName,
                    Size = size,
                    Description = description
                };

                _db.Properties.Add(property);
                await _db.SaveChangesAsync();
            }

            bool exists = await _db.PropertySales.AnyAsync(ps =>
                ps.CustomerId == customer.Id &&
                ps.PropertyId == property.Id &&
                ps.Unit == unit);

            if (exists) return;

            _db.PropertySales.Add(new PropertySale
            {
                Month = month,
                CustomerId = customer.Id,
                PropertyId = property.Id,
                AccountOfficerId = officerId, 

                Unit = unit,
                SellingPrice = sellingPrice,
                EquityDeposit = equity,
                Balance = balance,
                Status = ParseStatus(Get("STATUS")),
                SubscriptionPlan = ParsePlan(Get("PLAN")),
                StartDate = DateTime.UtcNow
            });

            await _db.SaveChangesAsync();
        }



        private async Task GetOrCreateUserAsync(string name, string email, string branch)
        {
            if (await _userManager.FindByEmailAsync(email) != null) return;

            if (!await _roleManager.RoleExistsAsync("AccountOfficer"))
                await _roleManager.CreateAsync(new IdentityRole("AccountOfficer"));

            var user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                FullName = name,
                Branch = branch,
                MustChangePassword = true
            };

            var tempPassword = $"Temp@{Guid.NewGuid():N}".Substring(0, 12);
            await _userManager.CreateAsync(user, tempPassword);
            await _userManager.AddToRoleAsync(user, "AccountOfficer");
        }

        private static decimal? TryParseDecimal(string input)
            => decimal.TryParse(input?.Replace(",", "").Replace("₦", ""), out var d) ? d : null;

        private static SubscriptionPlan ParsePlan(string plan)
            => plan?.ToLower().Contains("month") == true ? SubscriptionPlan.Monthly :
               plan?.ToLower().Contains("year") == true ? SubscriptionPlan.Yearly :
               SubscriptionPlan.Outright;

        private static PropertyStatus ParseStatus(string status)
            => status?.ToLower().Contains("sold") == true ? PropertyStatus.Sold :
               status?.ToLower().Contains("reserve") == true ? PropertyStatus.Reserved :
               PropertyStatus.Available;
    }


}

