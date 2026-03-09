using ClosedXML.Excel;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PropertySalesTracker.Data;
using PropertySalesTracker.Models;
using PropertySalesTracker.Services.Interface;

namespace PropertySalesTracker.Services
{
    public class AccountOfficerImportService : IAccountOfficerImportService
    {
        private readonly AppDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IEmailService _emailService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IConfiguration _configuration;


        public AccountOfficerImportService(
            AppDbContext db,
            IEmailService emailService,
            UserManager<ApplicationUser> userManager,
            IHttpContextAccessor httpContextAccessor,

            RoleManager<IdentityRole> roleManager,
            IConfiguration configuration)
        {
            _db = db;
            _userManager = userManager;
            _roleManager = roleManager;
            _emailService = emailService;
            _httpContextAccessor = httpContextAccessor;
            _configuration = configuration;
        }

        public async Task ImportAsync(IFormFile file)
        {
            await EnsureRoleExists("AccountOfficer");

            using var stream = file.OpenReadStream();
            using var workbook = new XLWorkbook(stream);
            var sheet = workbook.Worksheet(1);

            var headerRow = sheet.Row(1);
            var headers = headerRow.CellsUsed()
                .ToDictionary(
                    c => c.GetString().Trim().ToUpper(),
                    c => c.Address.ColumnNumber
                );

            string Get(IXLRow row, string header)
            {
                return headers.ContainsKey(header)
                    ? row.Cell(headers[header]).GetString().Trim()
                    : "";
            }

           

            var rows = sheet.RowsUsed().Skip(1);

            foreach (var row in rows)
            {
                var fullName = Get(row, "NAME");
                var email = Get(row, "EMAIL");
                var phone = Get(row, "PHONE");
                var department = Get(row, "DEPARTMENT");


                Console.WriteLine($"Processing: {fullName} - {email}");

                if (string.IsNullOrWhiteSpace(email))
                    continue;

                var user = await _userManager.FindByEmailAsync(email);

                const string tempPassword = "Temp@123!"; 

                if (user == null)
                {
                    user = new ApplicationUser
                    {
                        UserName = email,
                        Email = email,
                        FullName = fullName,
                        Branch = department,
                        MustChangePassword = true
                    };

                    var result = await _userManager.CreateAsync(user, tempPassword);

                    if (!result.Succeeded)
                    {
                        foreach (var error in result.Errors)
                            Console.WriteLine($"Identity error: {error.Description}");

                        continue;
                    }

                    await _userManager.AddToRoleAsync(user, "AccountOfficer");
                }

                var officer = await _db.AccountOfficers
                    .AsTracking() 
                    .FirstOrDefaultAsync(o => o.UserId == user.Id);

                bool officerCreated = false;

                if (officer == null)
                {
                    officer = new AccountOfficer
                    {
                        UserId = user.Id,
                        Name = fullName,
                        Email = email,
                        Phone = string.IsNullOrWhiteSpace(phone) ? "N/A" : phone,
                        Department = department
                    };

                    _db.AccountOfficers.Add(officer);
                    officerCreated = true;
                }
                else
                {
                    officer.Name = fullName;
                    officer.Email = email;
                    officer.Phone = string.IsNullOrWhiteSpace(phone) ? officer.Phone : phone;
                    officer.Department = department;
                }
                if (officerCreated)
                {
                    try
                    {
                        string subject = "Your Account Officer Login Credentials";

                        string body = $@"
<p>Dear {fullName},</p>

<p>Your account as an <strong>Account Officer</strong> has been created.</p>

<p>
<strong>Email:</strong> {email}<br/>
<strong>Temporary Password:</strong> {tempPassword}
</p>

<p>
Login here:
<a href='https://example.com
</a>
</p>

<p>Please change your password after your first login.</p>

<p>Regards,<br/>Property Sales Tracker Team</p>";

                        await _emailService.SendEmailAsync(email, subject, body);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Email failed: {ex.Message}");
                    }
                }
            }

            await _db.SaveChangesAsync();
        }




        private async Task EnsureRoleExists(string role)
        {
            if (!await _roleManager.RoleExistsAsync(role))
                await _roleManager.CreateAsync(new IdentityRole(role));
        }
    }
}
