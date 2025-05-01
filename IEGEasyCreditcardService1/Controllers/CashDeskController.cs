using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

using System.Net.Http.Headers;
using System.Net.Http;
using Consul;
using IEGEasyCreditcardService.Models;

namespace BlackFriday.Controllers
{
    [Produces("application/json")]
    [Route("api/CashDesk")]
    public class CashDeskController : Controller
    {

        private readonly ILogger<CashDeskController> _logger;
        private static  string creditcardServiceBaseAddress="http://iegeasycreditcardservice.azurewebsites.net/";

        public CashDeskController(ILogger<CashDeskController> logger)
        {
            _logger = logger;
        }

        [HttpGet]
        public IActionResult Get(string id)
        {
            return Content("OK");
        }

        [HttpPost]
        public IActionResult Post([FromBody]Basket basket)
        {
           _logger.LogError("TransactionInfo Creditcard: {0} Product:{1} Amount: {2}", new object[] { basket.CustomerCreditCardnumber, basket.Product, basket.AmountInEuro});
            var uri = GetCreditCardTransactionsURIFromConsul();

            //Mapping
            CreditcardTransaction creditCardTransaction = new CreditcardTransaction()
            {
                Amount = basket.AmountInEuro,
                CreditcardNumber = basket.CustomerCreditCardnumber,
                ReceiverName = basket.Vendor
            };


            creditcardServiceBaseAddress = uri.AbsoluteUri;
            HttpClient client = new HttpClient();
            client.BaseAddress = new Uri(creditcardServiceBaseAddress);
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            HttpResponseMessage response =  client.PostAsJsonAsync(creditcardServiceBaseAddress + "/api/CreditcardTransactions", creditCardTransaction).Result;
           // HttpResponseMessage response = client.PostAsJsonAsync(uri.AbsoluteUri + "/api/CreditcardTransactions", creditCardTransaction).Result;
            response.EnsureSuccessStatusCode();
           
            
            return CreatedAtAction("Get", new { id = System.Guid.NewGuid() }, creditCardTransaction);
        }

        /*
         * https://www.consul.io/
         * CreditcardTransactions.json 
         * {
            "service":{"name": "Payment/CreditcardTransaction",
            "Address": "https://localhost",
            "tags": ["CreditCard","Transaction"],"port": 56093,
            "check":	 {
                "id": "HealthCheckCreditCardService",
                "name": "HTTP API on port 5000",
                "http": "http://localhost:56093/api/Healthcheck",  
                "interval": "10s",
                "timeout": "1s"
  		            }
	            }
            }
        *c:\ consul agent -dev -enable-script-checks -config-dir=./config
        * http://localhost:8500/ui/dc1/services
         * 
         */

        private Uri GetCreditCardTransactionsURIFromConsul()
        {

            List<Uri> _serverUrls = new List<Uri>();
            var consuleClient = new ConsulClient(c => c.Address = new Uri("http://127.0.0.1:8500"));
            var services = consuleClient.Agent.Services().Result.Response;
            foreach (var service in services)
            {
                var isCreditCardApi = service.Value.Tags.Any(t => t == "CreditCard") ;
                if (isCreditCardApi)
                {
                    try
                    {
                        var serviceUri = new Uri($"{service.Value.Address}:{service.Value.Port}");
                        _serverUrls.Add(serviceUri);
                    }
                    catch (Exception)
                    {

                        ;
                    }

                }
            }
            return _serverUrls?.FirstOrDefault();
        }
    }
}