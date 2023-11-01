using System.ComponentModel.DataAnnotations;

namespace BestStoreApi.Models
{
    public class Contact
    {
        public int Id { get; set; }
        [MaxLength(100)]
        public string Firstname { get; set; } = "";
        [MaxLength(100)]
        public string Lastname { get; set; } = "";
        [MaxLength(100)]
        public string Email { get; set; } = "";
        [MaxLength(100)]
        public string Phone { get; set; } = "";

        public required Subject Subject { get; set; }
        public string Message { get; set; } = "";
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
