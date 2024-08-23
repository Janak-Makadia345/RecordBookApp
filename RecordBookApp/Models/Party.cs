using System.ComponentModel.DataAnnotations;

namespace RecordBookApp.Models
{
    public class Party
    {
        [Key]
        public int PartyId { get; set; }
        [Required]
        public string PartyType { get; set; }
        public ICollection<Record> Records { get; set; }
    }
}
