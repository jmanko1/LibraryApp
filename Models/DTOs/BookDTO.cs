﻿using BookApp.Models.Entities;
using System.ComponentModel.DataAnnotations;

namespace BookApp.Models.DTOs
{
    public class BookDTO
    {
        public int BookId { get; set; }
        public string Title { get; set; }
        public AuthorDTO Author { get; set; }
        public int AvailableCopies { get; set; }
    }
}
