using geeks.Models;

namespace geeks.Queries
{
    public class EventWithInvitations : Query<Event>
    {
        public string EventId { get; set; }

        public override Event Execute()
        {
            return Session.Include<Event>(e => e.Invitations).Load<Event>(EventId);
        }
    }
}