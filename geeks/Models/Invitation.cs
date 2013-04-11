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
        Unknown = 0,
        No = 1,
        Yes = 2
    }
}