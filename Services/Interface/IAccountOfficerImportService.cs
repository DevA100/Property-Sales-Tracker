namespace PropertySalesTracker.Services.Interface
{
    public interface IAccountOfficerImportService
    {
        Task ImportAsync(IFormFile file);
    }
}
