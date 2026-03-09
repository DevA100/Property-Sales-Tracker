using PropertySalesTracker.Dtos;
using PropertySalesTracker.DTOs;
using PropertySalesTracker.Models;

namespace PropertySalesTracker.Services.Interface
{
    public interface IPropertySaleService
    {
        Task<List<PropertySale>> GetAllAsync();
        Task<PropertySale> GetByIdAsync(int id);
        Task<PropertySale> CreateAsync(PropertySaleCreateDto dto);
        Task<PropertySale?> UpdateAsync(int id, PropertySaleUpdateDto dto);
        Task<bool> DeleteAsync(int id);
        Task ImportFromCsvAsync(Stream csvStream);
        Task ImportFromExcelAsync(Stream excelStream);

    }
}
