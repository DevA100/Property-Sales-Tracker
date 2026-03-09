using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace PropertySalesTracker.Models
{
    public class Property
    {
        public int Id { get; set; }

        [Required, MaxLength(100)]
        public string Name { get; set; }

        [MaxLength(100)]
        public string? Description { get; set; }  

        public string? Location { get; set; }     

        [MaxLength(50)]
        public string? Size { get; set; }         
        public PropertyStatus Status { get; set; } = PropertyStatus.Available;

        public int? Unit { get; set; }            

        [Display(Name = "Cost per Square Meter (₦)")]
        public string? CostPerSquare { get; set; } 

        [Display(Name = "Cost per Unit (₦)")]
        public decimal? CostPerUnit { get; set; }  

        [Display(Name = "Form Fee (₦)")]
        public decimal? FormFee { get; set; }     

        [Display(Name = "Selling Price (₦)")]
        [Required(ErrorMessage = "Selling price is required.")]
        public decimal SellingPrice { get; set; }  

        [Required]
        public string PropertyType { get; set; }  

        public ICollection<PropertySale> PropertySales { get; set; } = new List<PropertySale>();
    }
}
