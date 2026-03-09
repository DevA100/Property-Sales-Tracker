using PropertySalesTracker.Models;
using PropertySalesTracker.Models.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PropertySalesTracker.Services.Interface
{
    public interface ICustomerService
    {
        Task<Customer> CreateAsync(CustomerCreateDto dto);
        Task<Customer?> UpdateAsync(int id, CustomerUpdateDto dto);
        Task<List<Customer>> GetAllAsync();
        Task<Customer?> GetByIdAsync(int id);
        Task DeleteAsync(int id);
        Task<List<Customer>> GetAllByOfficerAsync(string? officerId);
        Task ImportAsync(IFormFile file);
        Task<byte[]> ExportCustomersAsync();
        Task<byte[]> DownloadImportTemplateAsync();


    }
}
