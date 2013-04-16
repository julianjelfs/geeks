namespace geeks.Models
{
    public class InvitationModel
    {
        public bool IsCurrentUser { get; set; }
        public string PersonId { get; set; }
        public string Email { get; set; }
        public int Rating { get; set; }
        public bool EmailSent { get; set; }
        public InvitationResponse Response { get; set; }
        public string EventId { get; set; }
    }
}