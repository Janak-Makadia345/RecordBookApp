using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

namespace RecordBookApp.Models
{
    public class Book
    {
        [Key]
        public int BookId { get; set; }

        public DateTime CreatedAt { get; set; }

        [Required]
        public string BookName { get; set; }

        public int UserId { get; set; }

        [ForeignKey("UserId")]
        public User User { get; set; }

        // Navigation property
        public ICollection<Record> Records { get; set; }
    }
}
