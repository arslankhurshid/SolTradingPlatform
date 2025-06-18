// Services/EasyCreditPaymentServiceImpl.cs

using Grpc.Core;
using EasyCreditPaymentService;
using Microsoft.Extensions.Logging;

namespace IEGEasyCreditcardService2.Services
{
    public class EasyCreditPaymentServiceImpl : EasyCreditPaymentService.EasyCreditPaymentService.EasyCreditPaymentServiceBase
    {
        private readonly ILogger<EasyCreditPaymentServiceImpl> _logger;

        public EasyCreditPaymentServiceImpl(ILogger<EasyCreditPaymentServiceImpl> logger)
        {
            _logger = logger;
        }

        public override async Task<CreditCardPaymentResponse> ProcessPayment(CreditCardPaymentRequest request, ServerCallContext context)
        {
            _logger.LogInformation($"Received payment request for card holder: {request.CardHolder}");

            // Basic validation
            if (string.IsNullOrEmpty(request.CardNumber) || 
                string.IsNullOrEmpty(request.CardHolder) || 
                string.IsNullOrEmpty(request.Expiry))
            {
                _logger.LogWarning("Invalid card details provided");
                return new CreditCardPaymentResponse
                {
                    Success = false,
                    TransactionId = string.Empty
                };
            }

            // Simple validation: approve if card number starts with '4'
            bool isApproved = request.CardNumber.StartsWith("4");
            
            if (isApproved)
            {
                var transactionId = Guid.NewGuid().ToString();
                _logger.LogInformation($"Payment approved for card ending in {request.CardNumber.Substring(request.CardNumber.Length - 4)}. Transaction ID: {transactionId}");
                return new CreditCardPaymentResponse
                {
                    Success = true,
                    TransactionId = transactionId
                };
            }
            else
            {
                _logger.LogWarning($"Payment rejected for card ending in {request.CardNumber.Substring(request.CardNumber.Length - 4)}");
                return new CreditCardPaymentResponse
                {
                    Success = false,
                    TransactionId = string.Empty
                };
            }
        }
    }
}