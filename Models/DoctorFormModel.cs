
using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace backend.Models
{
    public class DoctorFormModel
    {
        [Required]
        public string FirstName { get; set; }

        [Required]
        public string LastName { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        [Required]
        public string PrivateNumber { get; set; }

        [Required]
        public string Category { get; set; }

        [Required]
        public IFormFile Image { get; set; }

        [Required]
        public IFormFile CV { get; set; }

        public string Achievements { get; set; }




    }
}
