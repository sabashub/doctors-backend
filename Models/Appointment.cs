namespace backend.Models
{
    public class Appointment
    {
        public int Id { get; set; }
        public string UserId { get; set; } // Foreign key referencing User
        public int DoctorId { get; set; } // Foreign key referencing Doctor
        public DateTime Date { get; set; }
        public string Problem { get; set; }
    }

}

