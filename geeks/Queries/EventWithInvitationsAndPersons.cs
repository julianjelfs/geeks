using System.Linq;
using geeks.Models;

namespace geeks.Queries
{
    public class EventWithInvitationsAndPersons : Query<Event>
    {
        public string EventId { get; set; }

        public override Event Execute()
        {
            return Session
                .Include<Event>(e => e.Invitations.Select(i => i.PersonId))
                .Load<Event>(EventId);
        }
    }
}