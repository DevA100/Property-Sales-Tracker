using ClosedXML.Excel;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PropertySalesTracker.Data;
using PropertySalesTracker.Models;
using PropertySalesTracker.Models.DTOs;
using PropertySalesTracker.Services.Interface;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PropertySalesTracker.Services
{
    public class CustomerService : ICustomerService
    {
        private readonly AppDbContext _db;
        private readonly IEmailService _emailService;

        private readonly UserManager<ApplicationUser> _userManager;
        public CustomerService(AppDbContext db, IEmailService emailService, UserManager<ApplicationUser> userManager)
        {
            _db = db;
            _emailService = emailService;
            _userManager = userManager;
        }

        public async Task<Customer> CreateAsync(CustomerCreateDto dto)
        {
            var existingCustomer = await _db.Customers
                .AnyAsync(c => c.Email == dto.Email);

            if (existingCustomer)
                throw new InvalidOperationException("A customer with this email already exists.");

            var customer = new Customer
            {
                FirstName = dto.FirstName,
                LastName = dto.LastName,
                Email = dto.Email,
                Phone = dto.Phone,
                Country = dto.Country,
                IsDiaspora = dto.IsDiaspora,
                AccountOfficerId = dto.AccountOfficerId,
                Branch = dto.Branch,
                Unit = dto.Unit ?? 0,
                Size = dto.Size,
                FormFee = dto.FormFee
            };

            if (!dto.PropertyId.HasValue)
                throw new InvalidOperationException("Property must be selected");

            customer.PropertySales.Add(new PropertySale
            {
                PropertyId = dto.PropertyId.Value,
                SubscriptionPlan = dto.SubscriptionPlan,
                EquityDeposit = dto.EquityDeposit,
                Balance = dto.Balance,
                Month = dto.Month,
                Unit = dto.Unit ?? 0,
                Size = dto.Size,
                FormFee = dto.FormFee,
                Description = "Auto-created from customer registration",
                Status = PropertyStatus.Reserved,
                StartDate = DateTime.UtcNow
            });

            _db.Customers.Add(customer);
            await _db.SaveChangesAsync();

            return customer;
        }



        public async Task<Customer?> UpdateAsync(int id, CustomerUpdateDto dto)
        {
            var customer = await _db.Customers
                .Include(c => c.PropertySales)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (customer == null)
                return null;

            customer.FirstName = dto.FirstName;
            customer.LastName = dto.LastName;
            customer.Email = dto.Email;
            customer.Phone = dto.Phone;
            customer.Country = dto.Country;
            customer.IsDiaspora = dto.IsDiaspora;
            customer.AccountOfficerId = dto.AccountOfficerId;
            customer.Branch = dto.Branch;

            if (dto.PropertyId.HasValue)
            {
                var sale = customer.PropertySales?.FirstOrDefault() ?? new PropertySale { CustomerId = customer.Id };

                sale.PropertyId = dto.PropertyId.Value;
                sale.SubscriptionPlan = dto.SubscriptionPlan;
                sale.EquityDeposit = dto.EquityDeposit;
                sale.Balance = dto.Balance;
                sale.Month = dto.Month;
                sale.Status = dto.Status ?? PropertyStatus.Reserved;

                if (sale.Id == 0)
                    _db.PropertySales.Add(sale);
                else
                    _db.PropertySales.Update(sale);
            }

            await _db.SaveChangesAsync();
            return customer;
        }



        private DateTime CalculateNextDueDate(string plan)
        {
            var now = DateTime.UtcNow;
            return plan switch
            {
                "Monthly" => now.AddMonths(1),
                "Quarterly" => now.AddMonths(3),
                "Annual" => now.AddYears(1),
                _ => now.AddMonths(1)
            };
        }

        public async Task<List<Customer>> GetAllAsync()
        {
            return await _db.Customers
                .Include(c => c.AccountOfficer)
                .Include(c => c.PropertySales)
                    .ThenInclude(ps => ps.Property)
                .OrderBy(c => c.FirstName)
                .ToListAsync();
        }

        public async Task<List<Customer>> GetAllByOfficerAsync(string? officerId)
        {
            var query = _db.Customers
                .Include(c => c.AccountOfficer)
                .Include(c => c.PropertySales)
                    .ThenInclude(ps => ps.Property)
                .AsQueryable();

            if (!string.IsNullOrEmpty(officerId)) 
            {
                var officerUser = await _userManager.Users
                    .FirstOrDefaultAsync(u => u.Id == officerId); 

                if (officerUser != null)
                {
                    query = query.Where(c => c.AccountOfficerId == officerUser.Id);
                }
            }




            return await query.ToListAsync();
        }

        public async Task<Customer?> GetByIdAsync(int id)
        {
            return await _db.Customers
                .Include(c => c.AccountOfficer)
                .Include(c => c.PropertySales)
                    .ThenInclude(ps => ps.Property)
                .FirstOrDefaultAsync(c => c.Id == id);
        }



        public async Task ImportAsync(IFormFile file)
        {
            using var stream = file.OpenReadStream();
            using var workbook = new XLWorkbook(stream);
            var sheet = workbook.Worksheet(1);

            var rows = sheet.RowsUsed().Skip(1); 

            foreach (var row in rows)
            {
                string fullName = row.Cell(3).GetString().Trim();
                if (string.IsNullOrWhiteSpace(fullName))
                    continue;

                string firstName;
                string lastName;

                var parts = fullName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length == 1)
                {
                    firstName = parts[0];
                    lastName = "—"; 
                }
                else
                {
                    firstName = parts[0];
                    lastName = string.Join(" ", parts.Skip(1));
                }

                var phone = row.Cell(4).GetString().Trim();
                var email = row.Cell(5).GetString().Trim().ToLower();
                var branch = row.Cell(6).GetString().Trim();
                var size = row.Cell(8).GetString().Trim();
                var description = row.Cell(10).GetString().Trim();

                int unit = int.TryParse(row.Cell(11).GetString(), out var u) ? u : 1;
                decimal? formFee = TryGetDecimal(row.Cell(9));
                decimal? deposit = TryGetDecimal(row.Cell(13));
                decimal? sellingPrice = TryGetDecimal(row.Cell(14));
                decimal? outstanding = TryGetDecimal(row.Cell(15));

                var month = row.Cell(1).GetString().Trim();
                var plan = ParsePlan(row.Cell(16).GetString());
                var status = ParseStatus(row.Cell(17).GetString());

                var officerName = row.Cell(2).GetString().Trim();
                string? officerId = null;

                if (!string.IsNullOrWhiteSpace(officerName))
                {
                    var officer = await _db.AccountOfficers
                        .FirstOrDefaultAsync(a => EF.Functions.Like(a.Name, officerName));

                    officerId = officer?.UserId; 
                }


                var propertyName = row.Cell(7).GetString().Trim();
                if (string.IsNullOrWhiteSpace(propertyName))
                    continue;

                var property = await _db.Properties
                    .FirstOrDefaultAsync(p => p.Name == propertyName);

                if (property == null)
                    continue; 

                if (!string.IsNullOrWhiteSpace(email) &&
                    await _db.Customers.AnyAsync(c => c.Email == email))
                    continue;

                var customer = new Customer
                {
                    FirstName = firstName,
                    LastName = lastName,
                    Email = email,
                    Phone = phone,
                    Branch = branch,
                    AccountOfficerId = officerId,
                    Size = size,
                    Unit = unit,
                    FormFee = formFee
                };

                _db.Customers.Add(customer);
                await _db.SaveChangesAsync(); 

                customer.PropertySales.Add(new PropertySale
                {
                    CustomerId = customer.Id,
                    PropertyId = property.Id,
                    Month = month,
                    Unit = unit,
                    Size = size,
                    SellingPrice = sellingPrice,
                    EquityDeposit = deposit,
                    Balance = outstanding,
                    SubscriptionPlan = plan,
                    Status = status,
                    Description = description,
                    StartDate = DateTime.UtcNow
                });

                await _db.SaveChangesAsync();

                if (!string.IsNullOrWhiteSpace(email))
                {
                    try
                    {
                        string customerFullName = $"{firstName} {lastName}";
                        string subject = "Welcome to example Estate";
                        string body = $@"
            <p>Dear {customerFullName},</p>
            <p>Your registration with <strong> example </strong> was successful.</p>
            <p>Thank you for choosing  example.</p>
            <p>Warm regards,<br/>example  Team</p>";

                        await _emailService.SendEmailAsync(email, subject, body);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Email failed for {email}: {ex.Message}");
                    }
                }

            }
        }


        private SubscriptionPlan ParsePlan(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return SubscriptionPlan.Monthly;

            value = value.Trim().ToLower();

            return value switch
            {
                "monthly" => SubscriptionPlan.Monthly,
                "quarterly" => SubscriptionPlan.Quarterly,
                "annual" => SubscriptionPlan.Yearly,
                "outright" => SubscriptionPlan.Outright,
                "mortgaged" => SubscriptionPlan.Mortgaged,
                _ => SubscriptionPlan.Monthly
            };
        }

        private PropertyStatus ParseStatus(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return PropertyStatus.Reserved;

            value = value.Trim().ToLower();

            return value switch
            {
                "sold" => PropertyStatus.Sold,
                "reserved" => PropertyStatus.Reserved,
                "available" => PropertyStatus.Available,
                _ => PropertyStatus.Reserved
            };
        }

        private int TryGetInt(IXLCell cell)
        {
            return int.TryParse(cell.GetString(), out var v) ? v : 0;
        }

        private decimal? TryGetDecimal(IXLCell cell)
        {
            return decimal.TryParse(cell.GetString().Replace(",", ""), out var v)
                ? v
                : null;
        }



public async Task<byte[]> ExportCustomersAsync()
    {
        var customers = await _db.Customers
            .Include(c => c.AccountOfficer)
            .Include(c => c.PropertySales)
                .ThenInclude(ps => ps.Property)
            .ToListAsync();

        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("Customers");

        // Header
        sheet.Cell(1, 1).Value = "Customer Name";
        sheet.Cell(1, 2).Value = "Email";
        sheet.Cell(1, 3).Value = "Phone";
        sheet.Cell(1, 4).Value = "Account Officer";
        sheet.Cell(1, 5).Value = "Branch";
        sheet.Cell(1, 6).Value = "Property";
        sheet.Cell(1, 7).Value = "Unit";
        sheet.Cell(1, 8).Value = "Size";
        sheet.Cell(1, 9).Value = "Selling Price";
        sheet.Cell(1, 10).Value = "Outstanding Balance";
        sheet.Cell(1, 11).Value = "Status";
        sheet.Cell(1, 12).Value = "Plan";

        int row = 2;

        foreach (var customer in customers)
        {
            var sale = customer.PropertySales
                .OrderByDescending(ps => ps.Id)
                .FirstOrDefault();

            sheet.Cell(row, 1).Value = customer.FullName;
            sheet.Cell(row, 2).Value = customer.Email;
            sheet.Cell(row, 3).Value = customer.Phone;
            sheet.Cell(row, 4).Value = customer.AccountOfficer?.Name;
            sheet.Cell(row, 5).Value = customer.Branch;
            sheet.Cell(row, 6).Value = sale?.Property?.Name;
            sheet.Cell(row, 7).Value = sale?.Unit;
            sheet.Cell(row, 8).Value = sale?.Size;
            sheet.Cell(row, 9).Value = sale?.SellingPrice;
            sheet.Cell(row, 10).Value = sale?.Balance;
            sheet.Cell(row, 11).Value = sale?.Status.ToString();
            sheet.Cell(row, 12).Value = sale?.SubscriptionPlan.ToString();

            row++;
        }

        sheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

        public async Task<byte[]> DownloadImportTemplateAsync()
        {
            using var workbook = new XLWorkbook();
            var sheet = workbook.Worksheets.Add("Customer Import Template");

            sheet.Cell(1, 1).Value = "Month";
            sheet.Cell(1, 2).Value = "Account Officer Name";
            sheet.Cell(1, 3).Value = "Customer Full Name";
            sheet.Cell(1, 4).Value = "Phone";
            sheet.Cell(1, 5).Value = "Email";
            sheet.Cell(1, 6).Value = "Branch";
            sheet.Cell(1, 7).Value = "Property Name";
            sheet.Cell(1, 8).Value = "Size";
            sheet.Cell(1, 9).Value = "Form Fee";
            sheet.Cell(1, 10).Value = "Description";
            sheet.Cell(1, 11).Value = "Unit";
            sheet.Cell(1, 12).Value = "Unused Column (optional)";
            sheet.Cell(1, 13).Value = "Equity Deposit";
            sheet.Cell(1, 14).Value = "Selling Price";
            sheet.Cell(1, 15).Value = "Outstanding Balance";
            sheet.Cell(1, 16).Value = "Subscription Plan (Monthly/Quarterly/Annual/Outright/Mortgaged)";
            sheet.Cell(1, 17).Value = "Status (Reserved/Sold/Available)";

            sheet.Cell(2, 1).Value = "January";
            sheet.Cell(2, 2).Value = "John Doe";
            sheet.Cell(2, 3).Value = "Jane Smith";
            sheet.Cell(2, 4).Value = "08012345678";
            sheet.Cell(2, 5).Value = "jane@email.com";
            sheet.Cell(2, 6).Value = "Lagos Branch";
            sheet.Cell(2, 7).Value = " Estate Phase 1";
            sheet.Cell(2, 8).Value = "450 sqm";
            sheet.Cell(2, 9).Value = 50000;
            sheet.Cell(2, 10).Value = "Initial Allocation";
            sheet.Cell(2, 11).Value = 1;
            sheet.Cell(2, 13).Value = 2000000;
            sheet.Cell(2, 14).Value = 8000000;
            sheet.Cell(2, 15).Value = 6000000;
            sheet.Cell(2, 16).Value = "Monthly";
            sheet.Cell(2, 17).Value = "Reserved";

            sheet.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);

            return stream.ToArray();
        }


        public async Task DeleteAsync(int id)
        {
            var customer = await _db.Customers.FindAsync(id);
            if (customer != null)
            {
                _db.Customers.Remove(customer);
                await _db.SaveChangesAsync();
            }
        }
    }
}
