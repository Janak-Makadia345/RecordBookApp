using System.ComponentModel.DataAnnotations;

namespace RecordBookApp.Models
{
    public class Category
    {
        [Key]
        public int CategoryId { get; set; }

        [Required]
        public string Name { get; set; }

        [Required]
        public string Type { get; set; }

        public ICollection<Record> Records { get; set; }
    }
}
