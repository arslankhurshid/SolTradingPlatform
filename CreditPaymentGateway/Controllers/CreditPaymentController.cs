using Grpc.Net.Client;
using Microsoft.AspNetCore.Mvc;
using GrpcLoggingService;
using Grpc.Core;
using System.Text;
using EasyCreditPaymentService;
using OrderService;
using InventoryService;
using NotificationService;
using System.Net.Http;
using System.Net;

namespace CreditPaymentGateway.Controllers
{
    /// <summary>
    /// Controller responsible for handling credit card payments and order processing using the SAGA pattern.
    /// Implements distributed transaction management across multiple microservices.
    /// </summary>
    [ApiController]
    [Route("[controller]")]
    public class CreditPaymentController : ControllerBase
    {
        private readonly LogService.LogServiceClient _logClient;
        private readonly ILogger<CreditPaymentController> _logger;
        private static int _serviceIndex = 0;
        private readonly string[] _serviceUrls = new[]
        {
            "http://localhost:6001", // IEGEasyCreditcardService1
            "http://localhost:6002"  // IEGEasyCreditcardService2
        };

        /// <summary>
        /// Represents the state of a SAGA transaction across multiple services.
        /// Tracks the progress of each step in the distributed transaction.
        /// </summary>
        private class SagaTransaction
        {
            public string TransactionId { get; set; } = Guid.NewGuid().ToString();
            public string OrderId { get; set; } = string.Empty;
            public bool PaymentCompleted { get; set; }
            public bool InventoryReserved { get; set; }
            public bool OrderCreated { get; set; }
            public bool NotificationSent { get; set; }
            public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        }

        private readonly Dictionary<string, SagaTransaction> _activeTransactions = new();

        public CreditPaymentController(LogService.LogServiceClient logClient, ILogger<CreditPaymentController> logger)
        {
            _logClient = logClient;
            _logger = logger;
        }

        private GrpcChannel CreateChannel(string address)
        {
            _logger.LogInformation($"Creating gRPC channel for {address}");
            return GrpcChannel.ForAddress(address, new GrpcChannelOptions
            {
                HttpHandler = new SocketsHttpHandler
                {
                    EnableMultipleHttp2Connections = true,
                    KeepAlivePingPolicy = HttpKeepAlivePingPolicy.WithActiveRequests,
                    KeepAlivePingDelay = TimeSpan.FromSeconds(30),
                    KeepAlivePingTimeout = TimeSpan.FromSeconds(10),
                    PooledConnectionIdleTimeout = TimeSpan.FromMinutes(5),
                    PooledConnectionLifetime = TimeSpan.FromMinutes(10)
                },
                MaxReceiveMessageSize = 5 * 1024 * 1024, // 5 MB
                MaxSendMessageSize = 5 * 1024 * 1024, // 5 MB
                DisposeHttpClient = true
            });
        }

        /// <summary>
        /// Processes a credit card payment by attempting to connect to multiple payment services.
        /// Implements retry logic and load balancing between services.
        /// </summary>
        [HttpPost("pay")]
        public async Task<IActionResult> MakePayment([FromBody] ApiCreditCardPaymentRequest request)
        {
            var errorLog = new StringBuilder();
            const int maxAttemptsPerService = 2;

            for (int round = 0; round < _serviceUrls.Length; round++)
            {
                _serviceIndex = (_serviceIndex + 1) % _serviceUrls.Length;
                string currentServiceUrl = _serviceUrls[_serviceIndex];

                for (int retry = 0; retry < maxAttemptsPerService; retry++)
                {
                    try
                    {
                        errorLog.AppendLine($"Trying service: {currentServiceUrl}");
                        var channel = CreateChannel(currentServiceUrl);
                        var grpcClient = new EasyCreditPaymentService.EasyCreditPaymentService.EasyCreditPaymentServiceClient(channel);

                        var grpcRequest = new CreditCardPaymentRequest
                        {
                            CardNumber = request.CardNumber,
                            CardHolder = request.CardHolder,
                            Expiry = request.Expiration
                        };
                        errorLog.AppendLine("Sending gRPC request...");
                        var grpcResponse = await grpcClient.ProcessPaymentAsync(grpcRequest);
                        errorLog.AppendLine($"Received response: Success={grpcResponse.Success}");

                        if (grpcResponse.Success)
                        {
                            return Ok(new { 
                                Message = "Payment successful",
                                TransactionId = grpcResponse.TransactionId
                            });
                        }
                        else
                        {
                            await LogErrorAsync("Payment was unsuccessful", currentServiceUrl);
                        }
                    }
                    catch (RpcException ex)
                    {
                        await LogErrorAsync($"gRPC error: {ex.StatusCode} - {ex.Message}", currentServiceUrl);
                    }
                    catch (Exception ex)
                    {
                        await LogErrorAsync($"Unexpected error: {ex.Message}", currentServiceUrl);
                    }

                    await Task.Delay(200); // delay before retry
                }
            }

            return StatusCode(500, new
            {
                Message = "All payment services failed",
                DebugInfo = errorLog.ToString()
            });
        }

        /// <summary>
        /// Processes a complete order using the SAGA pattern:
        /// 1. Creates an order
        /// 2. Reserves inventory
        /// 3. Processes payment
        /// 4. Sends notification
        /// Implements compensation logic if any step fails.
        /// </summary>
        [HttpPost("process-order")]
        public async Task<IActionResult> ProcessOrder([FromBody] OrderProcessingRequest request)
        {
            var transaction = new SagaTransaction();
            _activeTransactions[transaction.TransactionId] = transaction;

            try
            {
                _logger.LogInformation($"Starting order processing for transaction {transaction.TransactionId}");

                // Step 1: Create Order
                _logger.LogInformation("Creating order...");
                using var orderChannel = CreateChannel("http://localhost:5000");
                var orderClient = new OrderService.OrderService.OrderServiceClient(orderChannel);
                
                var orderResponse = await orderClient.CreateOrderAsync(new CreateOrderRequest
                {
                    CustomerId = request.CustomerId,
                    TotalAmount = request.TotalAmount,
                    Items = { request.Items.Select(i => new OrderItem 
                    { 
                        ProductId = i.ProductId, 
                        Quantity = i.Quantity, 
                        Price = i.Price 
                    })}
                });

                if (!orderResponse.Success)
                {
                    throw new Exception($"Failed to create order: {orderResponse.Message}");
                }

                transaction.OrderId = orderResponse.OrderId;
                transaction.OrderCreated = true;
                _logger.LogInformation($"Order created successfully: {transaction.OrderId}");

                // Step 2: Check and Reserve Inventory
                _logger.LogInformation("Checking inventory...");
                using var inventoryChannel = CreateChannel("http://localhost:5004");
                var inventoryClient = new InventoryService.InventoryService.InventoryServiceClient(inventoryChannel);

                var stockResponse = await inventoryClient.CheckStockAsync(new CheckStockRequest
                {
                    Items = { request.Items.Select(i => new StockItem 
                    { 
                        ProductId = i.ProductId, 
                        Quantity = i.Quantity 
                    })}
                });

                if (!stockResponse.Success || stockResponse.Items.Any(i => !i.Available))
                {
                    throw new Exception("Insufficient stock for some items");
                }

                _logger.LogInformation("Reserving items...");
                var reserveResponse = await inventoryClient.ReserveItemsAsync(new ReserveItemsRequest
                {
                    OrderId = transaction.OrderId,
                    Items = { request.Items.Select(i => new StockItem 
                    { 
                        ProductId = i.ProductId, 
                        Quantity = i.Quantity 
                    })}
                });

                if (!reserveResponse.Success)
                {
                    throw new Exception($"Failed to reserve items: {reserveResponse.Message}");
                }

                transaction.InventoryReserved = true;
                _logger.LogInformation("Inventory reserved successfully");

                // Step 3: Process Payment
                var paymentSuccess = false;
                var paymentError = new StringBuilder();
                for (int round = 0; round < _serviceUrls.Length && !paymentSuccess; round++)
                {
                    _serviceIndex = (_serviceIndex + 1) % _serviceUrls.Length;
                    string currentServiceUrl = _serviceUrls[_serviceIndex];

                    try
                    {
                        _logger.LogInformation($"Attempting payment with service {currentServiceUrl}");
                        var channel = CreateChannel(currentServiceUrl);
                        var grpcClient = new EasyCreditPaymentService.EasyCreditPaymentService.EasyCreditPaymentServiceClient(channel);

                        var grpcRequest = new CreditCardPaymentRequest
                        {
                            CardNumber = request.CardNumber,
                            CardHolder = request.CardHolder,
                            Expiry = request.Expiration
                        };

                        var grpcResponse = await grpcClient.ProcessPaymentAsync(grpcRequest);
                        if (grpcResponse.Success)
                        {
                            paymentSuccess = true;
                            transaction.PaymentCompleted = true;
                            _logger.LogInformation("Payment processed successfully");
                            break;
                        }
                        else
                        {
                            paymentError.AppendLine($"Payment service {currentServiceUrl} rejected the payment");
                        }
                    }
                    catch (Exception ex)
                    {
                        paymentError.AppendLine($"Payment service {currentServiceUrl} error: {ex.Message}");
                        await LogErrorAsync($"Payment service error: {ex.Message}", currentServiceUrl);
                    }
                }

                if (!paymentSuccess)
                {
                    throw new Exception($"Payment failed: {paymentError}");
                }

                // Step 4: Send Success Notification
                var notificationChannel = CreateChannel("http://localhost:5005");
                var notificationClient = new NotificationService.NotificationService.NotificationServiceClient(notificationChannel);

                var notificationResponse = await notificationClient.SendNotificationAsync(new NotificationRequest
                {
                    RecipientId = request.CustomerId,
                    Type = NotificationType.OrderCompleted,
                    Message = $"Order {transaction.OrderId} has been processed successfully",
                    Metadata = 
                    {
                        { "OrderId", transaction.OrderId },
                        { "TransactionId", transaction.TransactionId }
                    }
                });

                if (!notificationResponse.Success)
                {
                    throw new Exception($"Failed to send notification: {notificationResponse.Message}");
                }

                transaction.NotificationSent = true;

                // Remove completed transaction
                _activeTransactions.Remove(transaction.TransactionId);

                return Ok(new
                {
                    Message = "Order processed successfully",
                    TransactionId = transaction.TransactionId,
                    OrderId = transaction.OrderId
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error processing order: {ex.Message}");
                await CompensateTransaction(transaction);
                return StatusCode(500, new { Message = "Order processing failed", Error = ex.Message });
            }
        }

        /// <summary>
        /// Implements the compensation logic for the SAGA pattern.
        /// Rolls back completed steps in reverse order if a transaction fails.
        /// </summary>
        private async Task CompensateTransaction(SagaTransaction transaction)
        {
            _logger.LogInformation($"Starting compensation for transaction {transaction.TransactionId}");

            // Step 1: Cancel Order if created
            if (transaction.OrderCreated)
            {
                try
                {
                    _logger.LogInformation($"Cancelling order {transaction.OrderId}...");
                    using var orderChannel = CreateChannel("http://localhost:5000");
                    var orderClient = new OrderService.OrderService.OrderServiceClient(orderChannel);
                    await orderClient.CancelOrderAsync(new CancelOrderRequest
                    {
                        OrderId = transaction.OrderId,
                        Reason = "Transaction failed"
                    });
                    _logger.LogInformation("Order cancelled successfully");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error cancelling order: {ex.Message}");
                }
            }

            // Step 2: Release Inventory if reserved
            if (transaction.InventoryReserved)
            {
                try
                {
                    _logger.LogInformation("Releasing inventory...");
                    using var inventoryChannel = CreateChannel("http://localhost:5004");
                    var inventoryClient = new InventoryService.InventoryService.InventoryServiceClient(inventoryChannel);
                    await inventoryClient.ReleaseItemsAsync(new ReleaseItemsRequest
                    {
                        OrderId = transaction.OrderId,
                        Items = { } // In a real implementation, we would need to store the items
                    });
                    _logger.LogInformation("Inventory released successfully");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error releasing inventory: {ex.Message}");
                }
            }

            // Step 3: Send Failure Notification
            try
            {
                _logger.LogInformation("Sending failure notification...");
                var notificationChannel = CreateChannel("http://localhost:5005");
                var notificationClient = new NotificationService.NotificationService.NotificationServiceClient(notificationChannel);
                await notificationClient.SendFailureNotificationAsync(new FailureNotificationRequest
                {
                    RecipientId = "customer", // In a real implementation, we would need to store the customer ID
                    ErrorMessage = "Transaction failed",
                    TransactionId = transaction.TransactionId,
                    ServiceName = "Order Processing"
                });
                _logger.LogInformation("Failure notification sent successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error sending failure notification: {ex.Message}");
            }

            // Remove the transaction
            _activeTransactions.Remove(transaction.TransactionId);
        }

        /// <summary>
        /// Logs errors to the centralized logging service.
        /// </summary>
        private async Task LogErrorAsync(string message, string serviceUrl)
        {
            try
            {
                var logRequest = new LogRequest
                {
                    Source = $"Gateway → {serviceUrl}",
                    Message = message,
                    Timestamp = DateTime.UtcNow.ToString("o")
                };

                var response = await _logClient.LogErrorAsync(logRequest);
                _logger.LogInformation($"Log successful: {response.Success}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to log: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Request model for credit card payment processing.
    /// </summary>
    public class ApiCreditCardPaymentRequest
    {
        public required string CardNumber { get; set; }
        public required string CardHolder { get; set; }
        public required string Expiration { get; set; }
    }

    /// <summary>
    /// Request model for complete order processing, including payment and inventory details.
    /// </summary>
    public class OrderProcessingRequest
    {
        public required string CustomerId { get; set; }
        public required string CardNumber { get; set; }
        public required string CardHolder { get; set; }
        public required string Expiration { get; set; }
        public required double TotalAmount { get; set; }
        public required List<OrderItemRequest> Items { get; set; }
    }

    /// <summary>
    /// Represents an item in an order request with product details and quantity.
    /// </summary>
    public class OrderItemRequest
    {
        public required string ProductId { get; set; }
        public required int Quantity { get; set; }
        public required double Price { get; set; }
    }
}
