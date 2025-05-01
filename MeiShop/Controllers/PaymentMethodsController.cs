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
        private static readonly string creditcardServiceBaseAddress = "https://localhost:7231/";


        public PaymentMethodsController(ILogger<PaymentMethodsController> logger)
        {
            _logger = logger;
        }
        [HttpGet]
        public async Task<IEnumerable<string>> GetAsync()
        {
            List<string> acceptedPaymentMethods = new List<string>() { "unknown"};
            _logger.LogError("Accepted Paymentmethods");
            var httpClient = new HttpClient();
            httpClient.BaseAddress = new Uri(creditcardServiceBaseAddress);
            httpClient.DefaultRequestHeaders.Accept.Clear();
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var response = await Policy
           .HandleResult<HttpResponseMessage>(message => !message.IsSuccessStatusCode)
           .WaitAndRetryAsync(3, i => TimeSpan.FromSeconds(2), (result, timeSpan, retryCount, context) =>
           {
               _logger.LogWarning($"Request failed with {result.Result.StatusCode}. Waiting {timeSpan} before next retry. Retry attempt {retryCount}");
           })
           .ExecuteAsync(() => httpClient.GetAsync(creditcardServiceBaseAddress + "api/AcceptedCreditCards"));

            if (response.IsSuccessStatusCode)
            {
                acceptedPaymentMethods = await response?.Content?.ReadFromJsonAsync<List<string>>();

                _logger.LogInformation("Response was successful.");
            }
            else
            {
                _logger.LogError($"Response failed. Status code {response.StatusCode}");
            }


            return acceptedPaymentMethods;
        }
    }
}
