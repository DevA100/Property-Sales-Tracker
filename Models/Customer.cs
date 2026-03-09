using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace PropertySalesTracker.Models
{
    public class Customer
    {
        public int Id { get; set; }

        [Required, MaxLength(50)]
        public string FirstName { get; set; }

        [Required, MaxLength(50)]
        public string LastName { get; set; }

        [EmailAddress]
        public string Email { get; set; }

        [Phone]
        public string Phone { get; set; }

        [MaxLength(50)]
        public string? Country { get; set; }

        public bool IsDiaspora { get; set; }

        public string? AccountOfficerId { get; set; }
        public AccountOfficer AccountOfficer { get; set; }

        [MaxLength(100)]
        public string Branch { get; set; }

        public string FullName => $"{FirstName} {LastName}";
        

        [Display(Name = "Form Fee (₦)")]
        public decimal? FormFee { get; set; } 
        public int Unit { get; set; }
        public string? Size { get; set; }

        public ICollection<PropertySale> PropertySales { get; set; } = new List<PropertySale>();
    }
}
