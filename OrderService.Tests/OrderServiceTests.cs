using Grpc.Net.Client;
using OrderService;
using Xunit;

namespace OrderService.Tests
{
    public class OrderServiceTests : IClassFixture<TestServerFixture>
    {
        private readonly GrpcChannel _channel;
        private readonly OrderService.OrderServiceClient _client;

        public OrderServiceTests(TestServerFixture fixture)
        {
            _channel = fixture.Channel;
            _client = new OrderService.OrderServiceClient(_channel);
        }

        [Fact]
        public async Task CreateOrder_Success()
        {
            // Arrange
            var request = new CreateOrderRequest
            {
                CustomerId = "test-customer-1",
                TotalAmount = 100.00,
                Items =
                {
                    new OrderItem
                    {
                        ProductId = "product-1",
                        Quantity = 2,
                        Price = 50.00
                    }
                }
            };

            // Act
            var response = await _client.CreateOrderAsync(request);

            // Assert
            Assert.True(response.Success);
            Assert.NotNull(response.OrderId);
            Assert.NotEmpty(response.OrderId);
        }

        [Fact]
        public async Task UpdateOrderStatus_Success()
        {
            // Arrange
            var createRequest = new CreateOrderRequest
            {
                CustomerId = "test-customer-2",
                TotalAmount = 200.00,
                Items =
                {
                    new OrderItem
                    {
                        ProductId = "product-2",
                        Quantity = 1,
                        Price = 200.00
                    }
                }
            };

            var createResponse = await _client.CreateOrderAsync(createRequest);
            var updateRequest = new UpdateOrderStatusRequest
            {
                OrderId = createResponse.OrderId,
                Status = OrderStatus.Processing
            };

            // Act
            var updateResponse = await _client.UpdateOrderStatusAsync(updateRequest);

            // Assert
            Assert.True(updateResponse.Success);
        }

        [Fact]
        public async Task CancelOrder_Success()
        {
            // Arrange
            var createRequest = new CreateOrderRequest
            {
                CustomerId = "test-customer-3",
                TotalAmount = 300.00,
                Items =
                {
                    new OrderItem
                    {
                        ProductId = "product-3",
                        Quantity = 3,
                        Price = 100.00
                    }
                }
            };

            var createResponse = await _client.CreateOrderAsync(createRequest);
            var cancelRequest = new CancelOrderRequest
            {
                OrderId = createResponse.OrderId,
                Reason = "Customer request"
            };

            // Act
            var cancelResponse = await _client.CancelOrderAsync(cancelRequest);

            // Assert
            Assert.True(cancelResponse.Success);
        }

        [Fact]
        public async Task UpdateOrderStatus_OrderNotFound()
        {
            // Arrange
            var request = new UpdateOrderStatusRequest
            {
                OrderId = "non-existent-order",
                Status = OrderStatus.Processing
            };

            // Act
            var response = await _client.UpdateOrderStatusAsync(request);

            // Assert
            Assert.False(response.Success);
            Assert.Contains("not found", response.Message);
        }
    }

    public class TestServerFixture : IDisposable
    {
        public GrpcChannel Channel { get; private set; }
        private readonly WebApplication _app;

        public TestServerFixture()
        {
            var builder = WebApplication.CreateBuilder();
            builder.Services.AddGrpc();
            builder.Services.AddLogging();

            _app = builder.Build();
            _app.MapGrpcService<Services.OrderServiceImpl>();

            var server = new TestServer(new WebApplicationFactory<Program>().Server);
            Channel = GrpcChannel.ForAddress(server.BaseAddress, new GrpcChannelOptions
            {
                HttpHandler = server.CreateHandler()
            });
        }

        public void Dispose()
        {
            Channel.Dispose();
            _app.DisposeAsync().AsTask().Wait();
        }
    }
} 