using System;
using System.ComponentModel.DataAnnotations;
using PropertySalesTracker.Models;

namespace PropertySalesTracker.Dtos
{
    public class PropertySaleUpdateDto
    {
        [Required]
        public int Id { get; set; }
        // Relations
        public int CustomerId { get; set; }
        public string AccountOfficerId { get; set; }
        public string Description { get; set; }

        [Display(Name = "Property")]
        public int? PropertyId { get; set; }

        [Display(Name = "Subscription Plan")]
        public SubscriptionPlan SubscriptionPlan { get; set; }

        [Display(Name = "Equity Deposit (₦)")]
        public decimal? EquityDeposit { get; set; }

        [Display(Name = "Balance (₦)")]
        public decimal? Balance { get; set; }

        [Display(Name = "Month")]
        [Range(1, 12)]
        public string Month { get; set; }

        [Display(Name = "Unit")]
        public int? Unit { get; set; }
        public decimal? SellingPrice { get; set; }

        [Display(Name = "Size")]
        public string Size { get; set; }

        [Display(Name = "Form Fee (₦)")]
        public decimal? FormFee { get; set; }

        [Display(Name = "Status")]
        public PropertyStatus? Status { get; set; }
        public bool IsQuarterly { get; set; }
        public bool IsMonthly { get; set; }
        public bool IsMortgage { get; set; }
        public bool IsOutright { get; set; }
        public bool IsMilestone { get; set; }
    }
}
