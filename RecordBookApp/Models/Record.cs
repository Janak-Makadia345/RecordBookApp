using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace RecordBookApp.Models
{
    public class Record
    {
        public int RecordId { get; set; }
        public DateTime Date { get; set; }
        public DateTime Time { get; set; }
        [Required]
        public decimal Amount { get; set; }
        public int BookId { get; set; }
        [ForeignKey("BookId")]
        public Book Book { get; set; }
        public int CategoryId { get; set; }
        [ForeignKey("CategoryId")]
        public Category Category { get; set; }
        public int PaymentId { get; set; }
        [ForeignKey("PaymentId")]
        public Payment Payment { get; set; }
        public byte[]? FileData { get; set; }
        public string? FileName { get; set; }
    }
}