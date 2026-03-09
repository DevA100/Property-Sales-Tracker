using System.ComponentModel.DataAnnotations;

namespace PropertySalesTracker.ViewModels
{
    public class RegisterViewModel
    {
        [Required, EmailAddress]
        public string Email { get; set; }

        [Required, Display(Name = "Full Name")]
        public string FullName { get; set; }

        [Required, DataType(DataType.Password)]
        public string Password { get; set; }

        [Display(Name = "Branch/Department")]
        public string Branch { get; set; }
    }
}
