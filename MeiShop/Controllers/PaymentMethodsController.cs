using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Polly;
using System.Net.Http.Headers;

namespace MeiShop.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PaymentMethodsController : ControllerBase
    {
        //https://docs.microsoft.com/en-us/aspnet/web-api/overview/advanced/calling-a-web-api-from-a-net-client
        private readonly ILogger<PaymentMethodsController> _logger;
        private readonly IConfiguration _configuration;
        private readonly HttpClient _httpClient;

        public PaymentMethodsController(ILogger<PaymentMethodsController> logger, IConfiguration configuration, IHttpClientFactory httpClientFactory)
        {
            _logger = logger;
            _configuration = configuration;
            _httpClient = httpClientFactory.CreateClient();
        }
        [HttpGet]
        public async Task<IEnumerable<string>> GetAsync()
        {
            List<string> acceptedPaymentMethods = new List<string>() { "unknown" };
            _logger.LogInformation("Fetching accepted payment methods");
            
            var creditcardServiceBaseAddress = _configuration["Services:CreditCardService:BaseUrl"];
            _httpClient.BaseAddress = new Uri(creditcardServiceBaseAddress);
            _httpClient.DefaultRequestHeaders.Accept.Clear();
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var response = await Policy
                .Handle<HttpRequestException>()
                .WaitAndRetryAsync(3, i => TimeSpan.FromSeconds(2), (result, timeSpan, retryCount, context) =>
                {
                    _logger.LogWarning($"Request failed with {result.Message}. Waiting {timeSpan} before next retry. Retry attempt {retryCount}");
                })
                .ExecuteAsync(() => _httpClient.GetAsync("api/AcceptedCreditCards"));

            if (response.IsSuccessStatusCode)
            {
                acceptedPaymentMethods = await response.Content.ReadFromJsonAsync<List<string>>();
                _logger.LogInformation("Successfully retrieved accepted payment methods");
            }
            else
            {
                _logger.LogError($"Failed to get payment methods. Status code: {response.StatusCode}");
            }

            return acceptedPaymentMethods;
        }
    }
}
