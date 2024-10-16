﻿using BookApp.Models.Entities;
using System.ComponentModel.DataAnnotations;

namespace BookApp.Models.DTOs
{
    public class LoanResponseDTO
    {
        public int LoanId { get; set; }
        public DateOnly DateFrom { get; set; }
        public DateOnly DateTo { get; set; }
        public string Status { get; set; }
        public ICollection<LoanBookDTO> Books { get; set; }
    }
}
