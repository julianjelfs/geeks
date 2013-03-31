namespace geeks.Models
{
    public class InvitationModel
    {
        public string PersonId { get; set; }
        public string Email { get; set; }
        public int Rating { get; set; }
        public bool EmailSent { get; set; }
    }
}