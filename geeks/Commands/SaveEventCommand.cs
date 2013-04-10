using System.Linq;
using Raven.Client.Linq;
using geeks.Models;
using geeks.Queries;
using geeks.Services;

namespace geeks.Commands
{
    public class SaveEventCommand : Command
    {
        public Event Event { get; set; }
        public IEmailer Emailer { get; set; }

        public override void Execute()
        {
            Session.Store(SendEmailToInvitees());
        }

        private Event SendEmailToInvitees()
        {
            var invitees = Event.Invitations.ToArray();
            
            //this looks redundant but it's not, it's a preload of 
            //all the person docs we will need in one go
            var users = Session.Query<Person>()
                               .Where(u => u.Id.In(from i in invitees
                                                   where !i.EmailSent
                                                   select i.PersonId));

            var organiser = Query(new PersonByUserId { UserId = CurrentUserId });
            
            foreach (var invitee in invitees.Where(i => !i.EmailSent))
            {
                Emailer.Invite(organiser, 
                               Session.Load<Person>(invitee.PersonId), 
                               Event);
                invitee.EmailSent = true;
            }
            Event.Invitations = invitees;
            return Event;
        }
    }
}