using System.ComponentModel.DataAnnotations;

namespace backend.DTO
{
    public class RegisterDto
    {
        [Required]
        public int Id { get; set; }
        public string FirstName { get; set; }
        [Required]

        public string LastName { get; set; }
        [Required]

        public string Email { get; set; }
        [Required]

        public string PrivateNumber { get; set; }
        [Required]


        public string Password { get; set; }


    }
}