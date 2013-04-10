using System.Linq;
using geeks.Models;

namespace geeks.Queries
{
    public class FriendsForAPerson : ListQuery<PersonFriend>
    {
        public string PersonId { get; set; }
        public string Search { get; set; }
        public bool Unrated { get; set; }

        public override ListResult<PersonFriend> Execute()
        {
            var person = ExecuteQuery(new PersonByIdWithFriends {Id = PersonId});

            var result = (from f in person.Friends
                          let p = Session.Load<Person>(f.PersonId)
                          where (string.IsNullOrEmpty(Search)
                                 || (p.EmailAddress != null && p.EmailAddress.Contains(Search))
                                 || (p.Name != null && p.Name.Contains(Search)))
                                && (f.Rating == 0 || !Unrated)
                          select new PersonFriend
                              {
                                  PersonId = p.Id,
                                  Name = p.Name,
                                  Email = p.EmailAddress,
                                  Rating = f.Rating,
                                  GravatarLink = GravatarHelper.GravatarHelper.CreateGravatarUrl(p.EmailAddress, 30, "http://dl.dropbox.com/u/26218407/logo-small.png", null, null, null)
                              }).OrderBy(friend => friend.Name);

            return PageFrom(result);
        }
    }
}