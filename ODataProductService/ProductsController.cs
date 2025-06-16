using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Formatter;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.AspNetCore.OData.Results;
using Microsoft.AspNetCore.OData.Routing.Controllers;

namespace ODataProductService
{
    /*
     * Minimal API: OData-Product-Service
    */

    // [controller] wird zu "Products", da die Klasse "ProductsController" heißt
    // also ist der Controller unter odata/Products erreichbar
    [Route("odata/[controller]")]
    public class ProductsController : ODataController
    {
        [EnableQuery]
        public IQueryable<Product> Get()
        {
            return FakeDatabase.Products.AsQueryable();
        }

        [EnableQuery]
        public SingleResult<Product> Get([FromODataUri] int key)
        {
            return SingleResult.Create(
                FakeDatabase.Products.Where(p => p.Id == key).AsQueryable());
        }
    }
}
