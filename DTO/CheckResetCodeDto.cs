using System.ComponentModel.DataAnnotations;

namespace backend.DTO
{
    public class CheckResetCodeDto
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        public string Code { get; set; }
    }
}