namespace geeks.Models
{
    public class Invitation
    {
        public string PersonId { get; set; }
        public bool EmailSent { get; set; }
        public InvitationResponse Response { get; set; }
    }

    public enum InvitationResponse
    {
        Yes,
        No
    }
}