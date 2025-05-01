using Microsoft.AspNetCore.Mvc;
using ProductCatalogService.FTP.Data;
using ProductCatalogService.FTP.Models;

namespace ProductCatalogService.FTP.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProductController : ControllerBase
    {
        [HttpGet]
        public ActionResult<IEnumerable<Product>> Get()
        {
            var products = ProductFtpRepository.GetProducts();
            return Ok(products);
        }
    }
}