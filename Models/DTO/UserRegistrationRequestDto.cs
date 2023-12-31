﻿using System.ComponentModel.DataAnnotations;

namespace VinderenApi.Models.DTO
{
    public class UserRegistrationRequestDto
    {
        [Required]
        [EmailAddress]
        public string? Email { get; set; }
        [Required]
        public string? Password { get; set; }
    }
}
