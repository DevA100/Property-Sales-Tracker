using System.Collections.Generic;
using System.Threading.Tasks;
using PropertySalesTracker.Models;

namespace PropertySalesTracker.Services.Interface
{
    public interface IPropertyService
    {
        Task<List<Property>> GetAllAsync();
        Task<Property?> GetByIdAsync(int id);
        Task CreateAsync(Property property);
        Task<int> ImportAsync(IFormFile file);

        Task UpdateAsync(Property property);
        Task DeleteAsync(int id);
    }
}
