using PropertySalesTracker.Models;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

public class AccountOfficer
{
    [Key]
    public string UserId { get; set; }   

    public string Name { get; set; }
    public string Email { get; set; }
    public string? Phone { get; set; }
    public string Department { get; set; }

    public ApplicationUser User { get; set; }

    public ICollection<Customer> Customers { get; set; } = new List<Customer>();
    public ICollection<PropertySale> PropertySales { get; set; } = new List<PropertySale>();
}
