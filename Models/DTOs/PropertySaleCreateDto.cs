using System;
using System.ComponentModel.DataAnnotations;

namespace PropertySalesTracker.DTOs
{
    public class PropertySaleCreateDto
    {
        [Required] public string Month { get; set; }
        [Required] public int CustomerId { get; set; }
        [Required] public string AccountOfficerId { get; set; }
        [Required] public int PropertyId { get; set; }

        public int Unit { get; set; } = 1;
        public decimal? FormFee { get; set; }
        public decimal? EquityDeposit { get; set; }
        public decimal? SellingPrice { get; set; }
        public decimal? Balance { get; set; }

        public bool IsQuarterly { get; set; }
        public bool IsMonthly { get; set; }
        public bool IsMortgage { get; set; }
        public bool IsOutright { get; set; }
        public bool IsMilestone { get; set; }
        public string Description { get; set; }
    }
}
