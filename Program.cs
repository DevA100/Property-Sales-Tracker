using Hangfire;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PropertySalesTracker.Data;
using PropertySalesTracker.Models;
using PropertySalesTracker.Services;
using PropertySalesTracker.Services.Interface;
using Hangfire.SqlServer;


var builder = WebApplication.CreateBuilder(args);


var config = builder.Configuration;

builder.Services.AddControllersWithViews();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(config.GetConnectionString("DefaultConnection")));

builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.Password.RequireDigit = false;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.Password.RequiredLength = 6;
})
.AddEntityFrameworkStores<AppDbContext>()
.AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.Cookie.HttpOnly = true;
    options.ExpireTimeSpan = TimeSpan.FromMinutes(60);
    options.SlidingExpiration = true;
});

builder.Services.AddHangfire(config =>
    config.UseSqlServerStorage(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddHangfireServer();

builder.Services.AddScoped<ICustomerService, CustomerService>();
builder.Services.AddHttpClient<IEmailService, EmailService>(); // ? CORRECT
builder.Services.AddScoped<ISmsService, SmsService_HabariPay>();
builder.Services.AddScoped<IReminderService, ReminderService>();
builder.Services.AddScoped<IPropertySaleService, PropertySaleService>();
builder.Services.AddScoped<IPropertyService, PropertyService>();
builder.Services.AddScoped<IAccountOfficerImportService, AccountOfficerImportService>();
builder.Services.AddHttpContextAccessor();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}

app.UseStaticFiles();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.UseHangfireDashboard("/hangfire"); 

RecurringJob.AddOrUpdate<IReminderService>(
    "daily-reminders",
    s => s.SendRemindersAsync(),
    "0 8 * * *",
    TimeZoneInfo.Local);


app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=Login}/{id?}");

using (var scope = app.Services.CreateScope())
{
    await SeedData.EnsureSeedData(scope.ServiceProvider);
}

app.Run();
