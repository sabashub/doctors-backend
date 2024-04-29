namespace backend.Models
{

    public class Admin
    {
        public int Id { get; set; }

        public string Email { get; set; }
        public string Password { get; set; }

        public string Type { get; set; }
    }

}