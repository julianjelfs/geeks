using System.Collections.Generic;
using System.Linq;
using Raven.Client.Linq;
using geeks.Models;

namespace geeks.Queries
{
    public class PeopleAlreadyImported : Query<Dictionary<string, Person>>
    {
        public HashSet<string> Emails { get; set; }

        public override Dictionary<string, Person> Execute()
        {
            var index = 0;
            var page = 128;
            var existingPeople = new Dictionary<string, Person>();
            while (Emails.Count > index * page)
            {
                foreach (var p in Session.Query<Person>()
                                         .Where(u => u.EmailAddress.In(Emails.Skip(index * page).Take(page))))
                {
                    if (!existingPeople.ContainsKey(p.EmailAddress.ToLower()))
                        existingPeople.Add(p.EmailAddress.ToLower(), p);
                }
                index++;
            }
            return existingPeople;
        }
    }
}