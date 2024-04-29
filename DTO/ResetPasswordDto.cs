using System.ComponentModel.DataAnnotations;

namespace backend.DTO
{
    public class ResetPasswordDto
    {
        public string Email { get; set; } // User's email
        public string NewPassword { get; set; } // New password
    }
}