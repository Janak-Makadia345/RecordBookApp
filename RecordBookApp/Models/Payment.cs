using System.ComponentModel.DataAnnotations;

namespace RecordBookApp.Models
{
    public class Payment
    {
        [Key]
        public int PaymentId { get; set; }

        [Required]
        public string Type { get; set; }

        public ICollection<Record> Records { get; set; }
    }
}
