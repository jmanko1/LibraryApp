﻿using System.ComponentModel.DataAnnotations;

namespace BookApp.Models.DTOs
{
    public class AuthorDTO
    {
        public int AuthorId { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
    }
}
