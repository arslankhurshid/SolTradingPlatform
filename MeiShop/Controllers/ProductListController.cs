using Microsoft.AspNetCore.Mvc;
using MeiShop.Models;
using System.Net.Http;
using System.Text.Json;

namespace MeiShop.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProductListController : ControllerBase
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<ProductListController> _logger;
        private readonly IConfiguration _configuration;

        public ProductListController(IHttpClientFactory httpClientFactory, ILogger<ProductListController> logger, IConfiguration configuration)
        {
            _httpClient = httpClientFactory.CreateClient();
            _logger = logger;
            _configuration = configuration;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Product>>> Get()
        {
            try
            {
                // Get products from both services
                var localProducts = await GetProductsFromService("LocalProductService");
                var ftpProducts = await GetProductsFromService("FtpProductService");

                // Combine and return all products
                var allProducts = localProducts.Concat(ftpProducts);
                return Ok(allProducts);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to fetch products from services");
                return StatusCode(500, $"Failed to fetch products: {ex.Message}");
            }
        }

        private async Task<IEnumerable<Product>> GetProductsFromService(string serviceName)
        {
            try
            {
                var baseUrl = _configuration[$"Services:{serviceName}:BaseUrl"];
                var response = await _httpClient.GetAsync($"{baseUrl}/api/products");
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();
                var products = JsonSerializer.Deserialize<List<Product>>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                return products ?? new List<Product>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to fetch products from {ServiceName}", serviceName);
                return new List<Product>();
            }
        }
    }
}