using System.Linq;
using Raven.Client;
using geeks.Models;

namespace geeks.Queries
{
    public class PersonByUserIdWithFriends : Query<Person>
    {
        public string UserId { get; set; }

        public override Person Execute()
        {
            return ExecuteQuery(new PersonByUserId {UserId = UserId, WithFriends = true});
        }
    }
    
    public class PersonByIdWithFriends : Query<Person>
    {
        public string Id { get; set; }

        public override Person Execute()
        {
             return Session.Include<Person>(p => p.Friends.Select(f => f.PersonId))
                .Load(Id);
        }
    }
}