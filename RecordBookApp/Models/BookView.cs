using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;


namespace RecordBookApp.Models
{
    public class BookView
    {
        public int BookId { get; set; }
        public string BookName { get; set; }
        public decimal NetBalance { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}

