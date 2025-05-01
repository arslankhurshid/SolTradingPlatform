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

        public ProductListController(IHttpClientFactory httpClientFactory)
        {
            _httpClient = httpClientFactory.CreateClient();
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Product>>> Get()
        {
            try
            {
                var response = await _httpClient.GetAsync("http://localhost:5216/api/product");
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();
                var products = JsonSerializer.Deserialize<List<Product>>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                return Ok(products);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Failed to fetch products: {ex.Message}");
            }
        }
    }
}