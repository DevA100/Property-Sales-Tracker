using System;
using System.ComponentModel.DataAnnotations;

namespace PropertySalesTracker.Models
{
    public class PropertySale
    {
        public int Id { get; set; }

        public string Month { get; set; }

        public int CustomerId { get; set; }
        public Customer Customer { get; set; }

        public string? AccountOfficerId { get; set; }
        public AccountOfficer AccountOfficer { get; set; }

        public int PropertyId { get; set; }
        public Property Property { get; set; }

        [Display(Name = "Form Fee (₦)")]
        public decimal? FormFee { get; set; } 
        public int Unit { get; set; }
        public string? Size { get; set; }
        public string Description { get; set; }

        [DataType(DataType.Currency)]
        public decimal? SellingPrice { get; set; }

        [DataType(DataType.Currency)]
        public decimal? EquityDeposit { get; set; }

        [DataType(DataType.Currency)]
        public decimal? Balance { get; set; }
        public decimal? CostPerUnit { get; set; }
        public decimal? CostPerSquare { get; set; }


        public DateTime StartDate { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        [Required]
        public SubscriptionPlan SubscriptionPlan { get; set; }

        public DateTime? LastPaymentDate { get; set; }

        public DateTime? NextDueDate
        {
            get
            {
                return SubscriptionPlan switch
                {
                    SubscriptionPlan.Monthly => StartDate.AddMonths(1),
                    SubscriptionPlan.Quarterly => StartDate.AddMonths(3),
                    SubscriptionPlan.Yearly => StartDate.AddYears(1),
                    SubscriptionPlan.Outright => null,
                    SubscriptionPlan.Mortgaged => null,
                    _ => null
                };
            }
        }

        public PropertyStatus Status { get; set; } = PropertyStatus.Available;
        public DateTime? DateSold { get; set; }

    }

    public enum PropertyStatus
    {
        Available = 0,
        Reserved = 1,
        Sold = 2
    }

    public enum SubscriptionPlan
    {
        Outright = 0,
        Monthly = 1,
        Quarterly = 2,
        Yearly = 3,
            Mortgaged = 4
    }

}