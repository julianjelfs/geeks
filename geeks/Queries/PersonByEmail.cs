using System.Linq;
using geeks.Models;

namespace geeks.Queries
{
    public class PersonByEmail : Query<Person>
    {
        public string Email { get; set; }
        public override Person Execute()
        {
            return Session.Query<Person>()
                          .FirstOrDefault(p => p.EmailAddress == Email);
        }
    }
}