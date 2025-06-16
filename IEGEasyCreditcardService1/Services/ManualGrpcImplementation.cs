using Grpc.Core;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace IEGEasyCreditcardService1
{
    public class ManualGrpcService
    {
        public class CreditCardPaymentRequest
        {
            public string CardNumber { get; set; }
            public string CardHolder { get; set; }
            public string Expiration { get; set; }
        }

        public class CreditCardPaymentResponse
        {
            public bool Success { get; set; }
            public string TransactionId { get; set; }
        }

        public Task<CreditCardPaymentResponse> ProcessPayment(CreditCardPaymentRequest request)
        {
            return Task.FromResult(new CreditCardPaymentResponse 
            { 
                Success = true,
                TransactionId = Guid.NewGuid().ToString()
            });
        }
    }
}