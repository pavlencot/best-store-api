using System.ComponentModel.DataAnnotations;

namespace BestStoreApi.Models
{
    public class ContactDto
    {
        [Required]
        [MaxLength(100)]
        public string Firstname { get; set; } = "";
        [Required]
        [MaxLength(100)]
        public string Lastname { get; set; } = "";
        [Required]
        [EmailAddress]
        [MaxLength(100)]
        public string Email { get; set; } = "";
        [MaxLength(100)]
        public string? Phone { get; set; }

        public int SubjectId { get; set; }
        [Required]
        [MinLength(20)]
        [MaxLength(4000)]
        public string Message { get; set; } = "";
    }
}
