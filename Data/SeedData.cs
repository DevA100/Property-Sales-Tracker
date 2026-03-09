using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using PropertySalesTracker.Models;
using System;
using System.Threading.Tasks;

namespace PropertySalesTracker.Data
{
    public static class SeedData
    {
        public static async Task EnsureSeedData(IServiceProvider services)
        {
            using var scope = services.CreateScope();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

            string[] roles = { "SuperAdmin", "Admin", "AccountOfficer" };
            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                    await roleManager.CreateAsync(new IdentityRole(role));
            }

            string superAdminEmail = "super@propertytracker.com";
            string password = "Super@123";

            var superAdmin = await userManager.FindByEmailAsync(superAdminEmail);
            if (superAdmin == null)
            {
                superAdmin = new ApplicationUser
                {
                    UserName = superAdminEmail,
                    Email = superAdminEmail,
                    FullName = "System Super Admin",
                    Branch = "Main",
                    EmailConfirmed = true
                };

                var result = await userManager.CreateAsync(superAdmin, password);
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(superAdmin, "SuperAdmin");
                }
            }
        }
    }
}
