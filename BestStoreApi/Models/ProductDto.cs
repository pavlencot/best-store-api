using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace BestStoreApi.Models
{
    public class ProductDto
    {
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = "";

        [Required]
        [MaxLength(100)]
        public string Brand { get; set; } = "";

        [MaxLength(400)]
        public string? Description { get; set; }

        [Required]
        public string Category { get; set; } = "";

        [Required]
        public decimal Price { get; set; }

        public IFormFile? ImageFile { get; set; }
    }
}
