using geeks.Models;
using System.Linq;
using geeks.Queries;
using geeks.Services;

namespace geeks.Commands
{
    public class RespondToInviteCommand : Command
    {
        public Event Event { get; set; }
        public InvitationResponse Response { get; set; }
        public string PersonId { get; set; }
        public IEmailer Emailer { get; set; }

        public override void Execute()
        {
            var invitation = Event.Invitations.FirstOrDefault(i => i.PersonId == PersonId);
            if (invitation != null)
            {
                if (invitation.Response != Response)
                {
                    var organiser = Session.Query<Person>().FirstOrDefault(p => p.UserId == Event.CreatedBy);
                    var responder = Session.Load<Person>(invitation.PersonId);
                    Emailer.UpdateInvitationResponse(Event, organiser, responder, Response);
                }
                invitation.Response = Response;
            }
        }
    }
}