using System;
using System.ComponentModel.DataAnnotations;
using PropertySalesTracker.Models; // ✅ for SubscriptionPlan enum, PropertyStatus, etc.

namespace PropertySalesTracker.Models.DTOs
{
    public class CustomerUpdateDto
    {
        public int Id { get; set; }

        // 🧍‍♂️ Customer Information
        [Required]
        [StringLength(100)]
        public string FirstName { get; set; }

        [Required]
        [StringLength(100)]
        public string LastName { get; set; }

        [EmailAddress]
        public string Email { get; set; }

        [Phone]
        public string Phone { get; set; }

        public string Country { get; set; }

        public bool IsDiaspora { get; set; }

        [Display(Name = "Branch")]
        public string Branch { get; set; }

        // 👤 Relationship info
        [Display(Name = "Account Officer")]
        public string? AccountOfficerId { get; set; }

        // 🏠 Property & Sale details
        [Display(Name = "Property")]
        public int? PropertyId { get; set; }

        [Display(Name = "Subscription Plan")]
        public SubscriptionPlan SubscriptionPlan { get; set; } = SubscriptionPlan.Monthly;

        [Display(Name = "Equity Deposit (₦)")]
        [Range(0, double.MaxValue)]
        public decimal? EquityDeposit { get; set; }

        [Display(Name = "Balance (₦)")]
        [Range(0, double.MaxValue)]
        public decimal? Balance { get; set; }

        [Display(Name = "Month")]
        [Range(1, 12)]
        public string? Month { get; set; }

        [Display(Name = "Next Due Date")]
        [DataType(DataType.Date)]
        public DateTime? NextDueDate { get; set; }

        [Display(Name = "Status")]
        public PropertyStatus? Status { get; set; }

        // 📏 New fields you mentioned
        [Display(Name = "Unit")]
        public int? Unit { get; set; }

        [Display(Name = "Size")]
        public string Size { get; set; }

        [Display(Name = "Form Fee (₦)")]
        public decimal? FormFee { get; set; }
        public decimal? CostPerSquare { get; set; }
        public decimal? CostPerUnit { get; set; }

    }
}
