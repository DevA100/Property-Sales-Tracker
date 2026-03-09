using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;
using PropertySalesTracker.Services.Interface;

namespace PropertySalesTracker.Services
{
    public class EmailService : IEmailService
    {
        private readonly HttpClient _httpClient;
        

        public EmailService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;

          
        }

        public async Task SendEmailAsync(string to, string subject, string htmlContent)
        {

            System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls12;

            var baseAddress = "https://example.com";

            using (var httpClient = new HttpClient())
            {
                // Add headers
                httpClient.DefaultRequestHeaders.Accept.Clear();
                httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                httpClient.DefaultRequestHeaders.Authorization =
    new AuthenticationHeaderValue(
       "Your_key"
    );
                // JSON body
                var body = new JObject
                {
                    ["from"] = new JObject { ["address"] = "Your_address" },
                    ["to"] = new JArray
                {
                    new JObject
                    {
                        ["email_address"] = new JObject
                        {
                            ["address"] = to,
                            ["name"] = to,

                        }
                    }
                },
                    ["subject"] = subject,
                    ["htmlbody"] = htmlContent
                };

                var jsonContent = new StringContent(body.ToString(), Encoding.UTF8, "application/json");

                try
                {
                    var response = await httpClient.PostAsync(baseAddress, jsonContent);

                    string result = await response.Content.ReadAsStringAsync();

                    Console.WriteLine($"Status: {(int)response.StatusCode} {response.ReasonPhrase}");
                    Console.WriteLine("Response content:");
                    Console.WriteLine(result);
                }
                catch (HttpRequestException ex)
                {
                    Console.WriteLine($"HTTP Error: {ex.Message}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Unexpected Error: {ex.Message}");
                }
            }
        }




    }
}





