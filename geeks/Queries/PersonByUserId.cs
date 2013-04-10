using System.Linq;
using Raven.Client;
using geeks.Models;

namespace geeks.Queries
{
    public class PersonByUserId : Query<Person>
    {
        public string UserId { get; set; }
        public bool WithFriends { get; set; }

        public override Person Execute()
        {
            var q = Session.Query<Person>();
            if(WithFriends)
                q = q.Include(p => p.Friends.Select(f => f.PersonId));
            return q.FirstOrDefault(person => person.UserId == UserId);
        }
    }
}