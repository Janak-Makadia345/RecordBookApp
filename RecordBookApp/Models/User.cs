using System.ComponentModel.DataAnnotations;

namespace RecordBookApp.Models
{
    public class User 
    {
        [Key]
        public int UserId { get; set; }

        [Required]
        public string Username { get; set; }

        [Required]
        public string FullName { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        public string Password { get; set; }

        [Required]
        public string ConfirmPassword { get; set; }

        public bool IsEmailVerified { get; set; }

        public string EmailVerificationToken { get; set; }

        // Navigation property
        public ICollection<Book> Books { get; set; }
    }
}
