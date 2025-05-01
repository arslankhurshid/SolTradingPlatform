using Microsoft.AspNetCore.Mvc;
using ProductCatalogService.Local.Data;
using ProductCatalogService.Local.Models;

namespace ProductCatalogService.Local.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProductController : ControllerBase
    {
        [HttpGet]
        public ActionResult<IEnumerable<Product>> Get()
        {
            var products = ProductRepository.GetProducts();
            return Ok(products);
        }
    }
}