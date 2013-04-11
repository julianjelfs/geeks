using geeks.Models;
using System.Linq;

namespace geeks.Commands
{
    public class RespondToInviteCommand : Command
    {
        public Event Event { get; set; }
        public InvitationResponse Response { get; set; }
        public string PersonId { get; set; }

        public override void Execute()
        {
            var invitation = Event.Invitations.FirstOrDefault(i => i.PersonId == PersonId);
            if (invitation != null)
            {
                invitation.Response = Response;
            }
        }
    }
}