using BestStoreApi.Models;
using BestStoreApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace BestStoreApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductsController : ControllerBase
    {
        private readonly ApplicationDbContext context;
        private readonly IWebHostEnvironment env;

        private readonly List<string> listCategiries = new List<string>()
        {
            "Phones", "Computers", "Accessories", "Printers", "Cameras", "Other"
        };

        public ProductsController(ApplicationDbContext context, IWebHostEnvironment env)
        {
            this.context = context;
            this.env = env;
        }

        [HttpGet("categories")]
        public IActionResult GetCategories()
        {
            return Ok(listCategiries);
        }

        [HttpGet]
        public IActionResult GetProducts()
        {
            var products = context.Products.ToList();

            return Ok(products);
        }

        [HttpGet("id")]
        public IActionResult GetProduct(int id)
        {
            var product = context.Products.Find(id);

            if(product == null)
            {
                return NotFound();
            }

            return Ok(product);
        }

        [Authorize(Roles = "admin")]
        [HttpPost]
        public IActionResult CreateProduct([FromForm]ProductDto productDto)
        {
            if(!listCategiries.Contains(productDto.Category))
            {
                ModelState.AddModelError("Category", "Please select a valid category");
                return BadRequest(ModelState);
            }

            if(productDto.ImageFile == null) 
            {
                ModelState.AddModelError("ImageFile", "The image file is required");
                return BadRequest(ModelState);
            }

            //save image on the server
            string imageFileName = DateTime.Now.ToString("yyyyMMddHHmmssfff");
            imageFileName += Path.GetExtension(productDto.ImageFile.FileName);

            string imagesFolder = env.WebRootPath + "/images/products/";

            using (var stream = System.IO.File.Create(imagesFolder + imageFileName))
            {
                productDto.ImageFile.CopyTo(stream);
            }

            //save image in the database
            Product product = new Product()
            {
                Name = productDto.Name,
                Brand = productDto.Brand,
                Category = productDto.Category,
                Price = productDto.Price,
                Description = productDto.Description ?? "",
                ImageFileName = imageFileName,
                CreatedAt = DateTime.Now
            };

            context.Products.Add(product);
            context.SaveChanges();

            return Ok(product);
        }

        [Authorize(Roles = "admin")]
        [HttpPut("{id}")]
        public IActionResult UpdateProduct(int id, [FromForm]ProductDto productDto)
        {
            if(!listCategiries.Contains(productDto.Category))
            {
                ModelState.AddModelError("Category", "Please enter a valid category");
                return BadRequest(ModelState);
            }

            var product = context.Products.Find(id);

            if (product == null)
            {
                return NotFound();
            }

            string imageFileName = product.ImageFileName;
            if(productDto.ImageFile != null)
            {
                //save
                imageFileName = DateTime.Now.ToString("yyyyMMddHHmmssfff");
                imageFileName += Path.GetExtension(productDto.ImageFile.FileName);

                string imagesFolder = env.WebRootPath + "/images/products/";
                using (var stream = System.IO.File.Create(imagesFolder + imageFileName))
                {
                    productDto.ImageFile.CopyTo(stream);
                }

                // delete old
                System.IO.File.Delete(imagesFolder + product.ImageFileName);

            }

            product.Name = productDto.Name;
            product.Brand = productDto.Brand;
            product.Price = productDto.Price;
            product.Category = productDto.Category;
            product.Description = productDto.Description ?? "";
            product.ImageFileName = imageFileName;

            context.SaveChanges();

            return Ok(product);
        }

        [Authorize(Roles = "admin")]
        [HttpDelete("{id}")]
        public IActionResult DeleteProduct(int id)
        {
            var product = context.Products.Find(id);

            if(product == null)
            {
                return NotFound();
            }

            string imagesFolder = env.WebRootPath + "/images/products/";
            System.IO.File.Delete(imagesFolder + product.ImageFileName);

            context.Products.Remove(product);
            context.SaveChanges();

            return Ok();
        }
    }
}
