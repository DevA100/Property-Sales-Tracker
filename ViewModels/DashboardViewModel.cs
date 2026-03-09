using PropertySalesTracker.Models;
using System.Collections.Generic;

namespace PropertySalesTracker.ViewModels
{
    public class DashboardViewModel
    {
        public int TotalCustomers { get; set; }
        public int TotalProperties { get; set; }
        public int SoldProperties { get; set; }
        public int AvailableProperties { get; set; }
        public int ReservedProperties { get; set; }
        public int TotalAccountOfficers { get; set; }

        public List<PropertySale> RecentSales { get; set; } = new();
    }
}
