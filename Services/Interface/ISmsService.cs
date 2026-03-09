namespace PropertySalesTracker.Services.Interface
{
    public interface ISmsService
    {

        Task SendSmsAsync(string phone, string message);

    }
}
