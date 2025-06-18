using Grpc.Core;
using OrderService;

namespace OrderService.Services
{
    /// <summary>
    /// Implementation of the Order Service that manages order creation, retrieval, and cancellation.
    /// Part of the SAGA pattern for distributed transaction management.
    /// </summary>
    public class OrderServiceImpl : OrderService.OrderServiceBase
    {
        private readonly ILogger<OrderServiceImpl> _logger;
        private readonly Dictionary<string, Order> _orders = new();

        public OrderServiceImpl(ILogger<OrderServiceImpl> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Creates a new order and stores it in the system.
        /// </summary>
        public override async Task<CreateOrderResponse> CreateOrder(CreateOrderRequest request, ServerCallContext context)
        {
            try
            {
                _logger.LogInformation("Creating new order");

                var order = new Order
                {
                    OrderId = Guid.NewGuid().ToString(),
                    CustomerId = request.CustomerId,
                    TotalAmount = request.TotalAmount,
                    Status = OrderStatus.Created,
                    CreatedAt = DateTime.UtcNow.ToString("o"),
                    Items = { request.Items }
                };

                _orders[order.OrderId] = order;

                return new CreateOrderResponse
                {
                    Success = true,
                    OrderId = order.OrderId,
                    Message = "Order created successfully"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating order");
                return new CreateOrderResponse
                {
                    Success = false,
                    Message = $"Error creating order: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// Retrieves an order by its ID.
        /// </summary>
        public override async Task<GetOrderResponse> GetOrder(GetOrderRequest request, ServerCallContext context)
        {
            try
            {
                _logger.LogInformation($"Getting order {request.OrderId}");

                if (_orders.TryGetValue(request.OrderId, out var order))
                {
                    return new GetOrderResponse
                    {
                        Success = true,
                        Order = order,
                        Message = "Order retrieved successfully"
                    };
                }

                return new GetOrderResponse
                {
                    Success = false,
                    Message = "Order not found"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting order {request.OrderId}");
                return new GetOrderResponse
                {
                    Success = false,
                    Message = $"Error getting order: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// Cancels an existing order and updates its status.
        /// </summary>
        public override async Task<CancelOrderResponse> CancelOrder(CancelOrderRequest request, ServerCallContext context)
        {
            try
            {
                _logger.LogInformation($"Cancelling order {request.OrderId}");

                if (_orders.TryGetValue(request.OrderId, out var order))
                {
                    order.Status = OrderStatus.Cancelled;
                    order.CancelledAt = DateTime.UtcNow.ToString("o");
                    order.CancellationReason = request.Reason;

                    return new CancelOrderResponse
                    {
                        Success = true,
                        Message = "Order cancelled successfully"
                    };
                }

                return new CancelOrderResponse
                {
                    Success = false,
                    Message = "Order not found"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error cancelling order {request.OrderId}");
                return new CancelOrderResponse
                {
                    Success = false,
                    Message = $"Error cancelling order: {ex.Message}"
                };
            }
        }
    }
} 