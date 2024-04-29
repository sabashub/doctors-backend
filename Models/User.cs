using backend.DTO;
using Microsoft.AspNetCore.Identity;
using System;
using System.ComponentModel.DataAnnotations;

namespace backend.Models
{
    public class User : IdentityUser
    {

        public override string Id { get; set; }
        public required string FirstName { get; set; }
        [Required]
        public required string LastName { get; set; }
        [Required]
        public required string PrivateNumber { get; set; }
        public DateTime DateCreated { get; set; } = DateTime.UtcNow;

        public string ResetCode { get; set; } = "";


    }
}