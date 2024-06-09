using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace RecordBookApp.Models
{
    public class RecordView
    {
        public DateTime Date { get; set; }

        public DateTime Time { get; set; }

        [Required]
        public decimal Amount { get; set; }
    }
}
