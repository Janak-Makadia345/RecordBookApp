using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

namespace RecordBookApp.Models
{
    public class BookView
    {
        [Required(ErrorMessage = "The Book Name field is required.")]
        public string BookName { get; set; }
    }
}
