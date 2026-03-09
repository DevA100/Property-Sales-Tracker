using PropertySalesTracker.Models;
using System;

namespace PropertySalesTracker.Models.DTOs
{
    public class CustomerCreateDto
    {
        // Customer fields
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string? Country { get; set; }
        public bool IsDiaspora { get; set; }
        public string AccountOfficerId { get; set; }
        public string? Branch { get; set; }

        // Sale fields
        public int? PropertyId { get; set; }
        public SubscriptionPlan SubscriptionPlan { get; set; }
        public decimal? EquityDeposit { get; set; }
        public decimal? Balance { get; set; }
        public string? Month { get; set; }
        public DateTime? NextDueDate { get; set; }
        public int? Unit { get; set; }
        public string? Size { get; set; }
        public decimal? FormFee { get; set; }
        public decimal? CostPerSquare { get; set; }
        public decimal? CostPerUnit { get; set; }

    }

    //public class CustomerUpdateDto : CustomerCreateDto
    //{
    //    public int Id { get; set; }
    //    public PropertyStatus? Status { get; set; }
    //    public int? Unit { get; internal set; }
    //    public string? Size { get; internal set; }
    //    public decimal? FormFee { get; internal set; }
    //    public decimal? CostPerSquare { get; set; }
    //    public decimal? CostPerUnit { get; set; }
    //}
}
