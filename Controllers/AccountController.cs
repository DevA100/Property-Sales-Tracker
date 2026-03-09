
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using PropertySalesTracker.Models;

using PropertySalesTracker.ViewModels;
using System.Net.Mail;
using System.Net;
using System.Threading.Tasks;
using PropertySalesTracker.Data;
using PropertySalesTracker.Services.Interface;
using PropertySalesTracker.Services;
using Microsoft.EntityFrameworkCore;
using ClosedXML.Excel;

namespace PropertySalesTracker.Controllers
{
    public class AccountController : Controller
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly AppDbContext _context;
        private readonly IEmailService _emailService;
        private readonly IAccountOfficerImportService _accountOfficerImportService;


        public AccountController(
            IEmailService emailService,
AppDbContext context,
            SignInManager<ApplicationUser> signInManager,
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager, IAccountOfficerImportService accountOfficerImportService)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _roleManager = roleManager;
            _context = context;
            _emailService = emailService;
            _accountOfficerImportService = accountOfficerImportService;

        }


        [HttpGet]
        public IActionResult Login(string returnUrl = "/")
        {
            return View(new LoginViewModel { ReturnUrl = returnUrl });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel vm)
        {
            var user = await _userManager.FindByEmailAsync(vm.Email);
            if (user == null)
            {
                ModelState.AddModelError("", "Invalid login attempt.");
                return View(vm);
            }

            var result = await _signInManager.PasswordSignInAsync(user, vm.Password, vm.RememberMe, false);
            if (result.Succeeded)
            {
                var roles = await _userManager.GetRolesAsync(user);
                if (roles.Contains("SuperAdmin") || roles.Contains("Admin") || roles.Contains("AccountOfficer"))
                    return RedirectToAction("Index", "Dashboard");

                return RedirectToAction("Index", "Dashboard");
            }

            ModelState.AddModelError("", "Invalid email or password.");
            return View(vm);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Login");
        }


        [Authorize(Roles = "SuperAdmin,Admin")]
        [HttpGet]
        public IActionResult CreateAdmin()
        {
            return View(new RegisterViewModel());
        }

        [Authorize(Roles = "SuperAdmin,Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateAdmin(RegisterViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var user = new ApplicationUser
            {
                UserName = model.Email,
                Email = model.Email,
                FullName = model.FullName,
                Branch = model.Branch
            };

            var result = await _userManager.CreateAsync(user, model.Password);
            if (result.Succeeded)
            {
                await EnsureRoleExists("Admin");
                await _userManager.AddToRoleAsync(user, "Admin");

                string loginUrl = $"{Request.Scheme}://{Request.Host}/Account/Login";
                string subject = "Your Admin Account Credentials";
                string body = $@"
            <p>Dear {model.FullName},</p>
            <p>Your <strong>Admin</strong> account has been created successfully.</p>
            <p><strong>Login Email:</strong> {model.Email}<br/>
               <strong>Password:</strong> {model.Password}</p>
            <p>You can log in here: <a href='https://Example.com</a></p>
            <p>Regards,<br/>Property Sales Tracker Team</p>
        ";

                await _emailService.SendEmailAsync(model.Email, subject, body);

                TempData["Success"] = $"Admin account for {model.FullName} created and emailed successfully.";
                return RedirectToAction("Index", "Dashboard");
            }

            foreach (var error in result.Errors)
                ModelState.AddModelError("", error.Description);

            return View(model);
        }



        [Authorize(Roles = "SuperAdmin,Admin")]
        [HttpGet]
        public IActionResult CreateAccountOfficer()
        {
            return View(new RegisterViewModel());
        }

        [Authorize(Roles = "SuperAdmin,Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateAccountOfficer(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = new ApplicationUser
            {
                UserName = model.Email,
                Email = model.Email,
                FullName = model.FullName,
                Branch = model.Branch
            };

            var result = await _userManager.CreateAsync(user, model.Password);
            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                    ModelState.AddModelError("", error.Description);

                return View(model);
            }

            await EnsureRoleExists("AccountOfficer");
            await _userManager.AddToRoleAsync(user, "AccountOfficer");

            bool officerExists = await _context.AccountOfficers
    .AnyAsync(o => o.UserId == user.Id);


            if (!officerExists)
            {
                _context.AccountOfficers.Add(new AccountOfficer
                {
                    Name = model.FullName,
                    Email = model.Email,
                    Department = model.Branch,
                    Phone = "N/A",
                    UserId = user.Id
                });

                await _context.SaveChangesAsync();
            }

            string loginUrl = $"{Request.Scheme}://{Request.Host}/Account/Login";
            string subject = "Your Account Officer Login Credentials";

            string body = $@"
<p>Dear {model.FullName},</p>
<p>Your account as an <strong>Account Officer</strong> has been created successfully.</p>
<p><strong>Login Email:</strong> {user.Email}<br/>
<strong>Password:</strong> {model.Password}</p>
<p>You can log in here: <a href='https://Example.com</a></p>
<p>Please change your password after your first login.</p>
<p>Regards,<br/>Property Sales Tracker Team</p>";

            await _emailService.SendEmailAsync(user.Email, subject, body);

            TempData["Success"] = $"Account Officer {model.FullName} created and emailed successfully.";
            return RedirectToAction("Index", "Dashboard");
        }


        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ForgotPassword(string Email)
        {
            if (string.IsNullOrEmpty(Email))
            {
                TempData["Message"] = "Please enter your email.";
                return View();
            }

            var user = await _userManager.FindByEmailAsync(Email);
            if (user == null)
            {
                TempData["Message"] = "Email not found in the system.";
                return View();
            }

            // Generate password reset token
            var newPassword = GenerateTemporaryPassword();
            var resetToken = await _userManager.GeneratePasswordResetTokenAsync(user);
            var result = await _userManager.ResetPasswordAsync(user, resetToken, newPassword);

            if (result.Succeeded)
            {
                await SendPasswordByEmail(Email, newPassword);
                TempData["Message"] = "A new password has been sent to your email.";
            }
            else
            {
                TempData["Message"] = "An error occurred while resetting your password.";
            }

            return View();
        }

        // Generate a temporary password (e.g., 8-char random)
        private string GenerateTemporaryPassword()
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            var random = new Random();
            return new string(Enumerable.Repeat(chars, 8).Select(s => s[random.Next(s.Length)]).ToArray());
        }

        private async Task SendPasswordByEmail(string toEmail, string newPassword)
        {
            string subject = "Your Password Has Been Reset";
            string body = $@"
                <p>Hello,</p>
                <p>Your new temporary password is: <b>{newPassword}</b></p>
                <p>Please change it after logging in.</p>
                <p>Best regards,<br><b>Property Sales Tracker Team</b></p>
            ";
            await _emailService.SendEmailAsync(toEmail, subject, body);
        }



        [HttpGet]
        public IActionResult ChangePassword() => View();

        [HttpPost]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return RedirectToAction("Login");

            var result = await _userManager.ChangePasswordAsync(user, model.OldPassword, model.NewPassword);

            if (result.Succeeded)
            {
                TempData["Message"] = "Password changed successfully!";
                return RedirectToAction("Login");
            }

            foreach (var error in result.Errors)
                ModelState.AddModelError("", error.Description);

            return View(model);
        }





        [HttpPost]
        public async Task<IActionResult> ImportAccountOfficers(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                TempData["Error"] = "Please select a valid Excel file.";
                return RedirectToAction("Index", "Dashboard");
            }

            await _accountOfficerImportService.ImportAsync(file);

            TempData["Success"] = "Account officers imported successfully.";
            return RedirectToAction("Index", "Dashboard");
        }


        [HttpGet]
        public IActionResult DownloadAccountOfficerTemplate()
        {
            using var workbook = new XLWorkbook();
            var sheet = workbook.Worksheets.Add("Template");

            
            sheet.Cell(1, 1).Value = "NAME";
            sheet.Cell(1, 2).Value = "EMAIL";
            sheet.Cell(1, 3).Value = "PHONE";
            sheet.Cell(1, 4).Value = "DEPARTMENT";

            sheet.Cell(2, 1).Value = "John Doe";
            sheet.Cell(2, 2).Value = "john@company.com";
            sheet.Cell(2, 3).Value = "08012345678";
            sheet.Cell(2, 4).Value = "Lagos";

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            stream.Position = 0;

            return File(
                stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "AccountOfficerTemplate.xlsx");
        }







        private async Task EnsureRoleExists(string roleName)
        {
            if (!await _roleManager.RoleExistsAsync(roleName))
                await _roleManager.CreateAsync(new IdentityRole(roleName));
        }
    }
}

