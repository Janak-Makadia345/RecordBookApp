using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace RecordBookApp.Models
{
    public class User : IdentityUser<string>
    {
        [Key]
        public int UserId { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        public string Password { get; set; }

        // Navigation property
        public ICollection<Book> Books { get; set; }
    }
}
