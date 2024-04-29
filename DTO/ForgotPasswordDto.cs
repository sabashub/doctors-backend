using System.ComponentModel.DataAnnotations;

namespace backend.DTO
{
    public class ForgotPasswordDto
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }
    }
}
