using System.ComponentModel.DataAnnotations;

namespace PropertySalesTracker.ViewModels
{
    public class LoginViewModel
    {
        [Required, EmailAddress(ErrorMessage ="invalid Email address")] public string Email { get; set; }
        [Required(ErrorMessage ="password field is required")] public string Password { get; set; }
        public bool RememberMe { get; set; }
        public string ReturnUrl { get; set; }

    }
}
