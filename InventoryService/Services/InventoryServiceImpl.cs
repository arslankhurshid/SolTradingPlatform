using Grpc.Core;
using InventoryService;

namespace InventoryService.Services
{
    /// <summary>
    /// Implementation of the Inventory Service that manages stock levels and reservations.
    /// Part of the SAGA pattern for distributed transaction management.
    /// </summary>
    public class InventoryServiceImpl : InventoryService.InventoryServiceBase
    {
        private readonly ILogger<InventoryServiceImpl> _logger;
        private readonly Dictionary<string, StockItem> _inventory = new();

        public InventoryServiceImpl(ILogger<InventoryServiceImpl> logger)
        {
            _logger = logger;
            // Initialize with our test product and some additional products
            _inventory["PROD1"] = new StockItem { ProductId = "PROD1", Quantity = 100 };
            _inventory["product-1"] = new StockItem { ProductId = "product-1", Quantity = 100 };
            _inventory["product-2"] = new StockItem { ProductId = "product-2", Quantity = 50 };
            _inventory["product-3"] = new StockItem { ProductId = "product-3", Quantity = 75 };
        }

        /// <summary>
        /// Checks the availability of items in the inventory.
        /// </summary>
        public override async Task<CheckStockResponse> CheckStock(CheckStockRequest request, ServerCallContext context)
        {
            try
            {
                _logger.LogInformation("Checking stock for items");
                var stockStatuses = new List<StockStatus>();

                foreach (var item in request.Items)
                {
                    if (_inventory.TryGetValue(item.ProductId, out var stockItem))
                    {
                        stockStatuses.Add(new StockStatus
                        {
                            ProductId = item.ProductId,
                            Available = stockItem.Quantity >= item.Quantity,
                            AvailableQuantity = stockItem.Quantity
                        });
                    }
                    else
                    {
                        stockStatuses.Add(new StockStatus
                        {
                            ProductId = item.ProductId,
                            Available = false,
                            AvailableQuantity = 0
                        });
                    }
                }

                return new CheckStockResponse
                {
                    Success = true,
                    Items = { stockStatuses },
                    Message = "Stock check completed"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking stock");
                return new CheckStockResponse
                {
                    Success = false,
                    Message = $"Error checking stock: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// Reserves items in the inventory for an order.
        /// </summary>
        public override async Task<ReserveItemsResponse> ReserveItems(ReserveItemsRequest request, ServerCallContext context)
        {
            try
            {
                _logger.LogInformation($"Reserving items for order {request.OrderId}");

                foreach (var item in request.Items)
                {
                    if (!_inventory.TryGetValue(item.ProductId, out var stockItem))
                    {
                        return new ReserveItemsResponse
                        {
                            Success = false,
                            Message = $"Product {item.ProductId} not found in inventory"
                        };
                    }

                    if (stockItem.Quantity < item.Quantity)
                    {
                        return new ReserveItemsResponse
                        {
                            Success = false,
                            Message = $"Insufficient stock for product {item.ProductId}"
                        };
                    }

                    stockItem.Quantity -= item.Quantity;
                    stockItem.ReservedQuantity += item.Quantity;
                }

                return new ReserveItemsResponse
                {
                    Success = true,
                    Message = "Items reserved successfully"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error reserving items for order {request.OrderId}");
                return new ReserveItemsResponse
                {
                    Success = false,
                    Message = $"Error reserving items: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// Releases previously reserved items back to the inventory.
        /// </summary>
        public override async Task<ReleaseItemsResponse> ReleaseItems(ReleaseItemsRequest request, ServerCallContext context)
        {
            try
            {
                _logger.LogInformation($"Releasing items for order {request.OrderId}");

                foreach (var item in request.Items)
                {
                    if (_inventory.TryGetValue(item.ProductId, out var stockItem))
                    {
                        stockItem.Quantity += item.Quantity;
                        stockItem.ReservedQuantity -= item.Quantity;
                    }
                }

                return new ReleaseItemsResponse
                {
                    Success = true,
                    Message = "Items released successfully"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error releasing items for order {request.OrderId}");
                return new ReleaseItemsResponse
                {
                    Success = false,
                    Message = $"Error releasing items: {ex.Message}"
                };
            }
        }
    }

    /// <summary>
    /// Internal model representing a stock item in the inventory.
    /// </summary>
    public class StockItem
    {
        public string ProductId { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public int ReservedQuantity { get; set; }
    }
} 