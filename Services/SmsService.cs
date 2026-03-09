using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using PropertySalesTracker.Services.Interface;

namespace PropertySalesTracker.Services
{
    public class SmsService_HabariPay : ISmsService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private readonly string _senderId;
        private readonly string _endpoint;

        public SmsService_HabariPay(IConfiguration config)
        {
            _httpClient = new HttpClient();
            _apiKey = config["ApiKey"];
            _senderId = config["SenderId"];
            _endpoint = config["Endpoint"] ?? "https://example.com";
        }

        public async Task SendSmsAsync(string to, string message)
        {
            var payload = new
            {
                sender_id = _senderId,
                messages = new[]
                {
                    new { phone_number = to, message = message }
                }
            };

            string jsonBody = JsonSerializer.Serialize(payload);
            var content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);

            Console.WriteLine("📤 Sending SMS via  ()...");
            Console.WriteLine($"Endpoint: {_endpoint}");
            Console.WriteLine($"Payload: {jsonBody}");

            // ✅ Send the request
            var response = await _httpClient.PostAsync(_endpoint, content);
            var responseText = await response.Content.ReadAsStringAsync();

            Console.WriteLine($"📩 Response: {(int)response.StatusCode} {response.ReasonPhrase}");
            Console.WriteLine($"Response Body: {responseText}");

            if (!response.IsSuccessStatusCode)
                throw new Exception($" () SMS failed: {responseText}");
        }
    }
}
