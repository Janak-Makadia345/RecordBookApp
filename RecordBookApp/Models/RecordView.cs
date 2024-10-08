﻿using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace RecordBookApp.Models
{
    public class RecordView
    {
        public DateTime Date { get; set; }
        public DateTime Time { get; set; }
        [Required]
        public decimal Amount { get; set; }
    
        [Required]
        public int CategoryId { get; set; }
        [Required]
        public int PaymentId { get; set; }
        public IFormFile? File { get; set; } // For file upload
    }
}
