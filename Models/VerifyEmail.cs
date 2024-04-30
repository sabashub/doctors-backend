namespace backend.Models
{

    public class VerifyMail
    {
        public int Id { get; set; }

        public string Token { get; set; }
        public string Email { get; set; }
    }

}