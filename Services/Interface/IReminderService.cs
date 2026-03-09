namespace PropertySalesTracker.Services.Interface
{
    public interface IReminderService
    {
        Task SendRemindersAsync();
        Task SendSingleReminderAsync(int saleId);

    }
}
