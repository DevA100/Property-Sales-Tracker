using Microsoft.AspNetCore.Identity;

namespace PropertySalesTracker.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string FullName { get; set; }
        public string? Branch { get; set; }

        public bool MustChangePassword { get; set; } = true;

    }
}
