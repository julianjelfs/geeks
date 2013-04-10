using System;
using System.Collections.Generic;
using System.Linq;
using geeks.Models;
using geeks.Queries;

namespace geeks.Commands
{
    public class AddFriendCommand : Command
    {
        public string Name { get; set; }
        public string Email { get; set; }

        public override void Execute()
        {
            var me = Query(new PersonByUserIdWithFriends {UserId = CurrentUserId});
            var person = CreatePersonDocumentIfNecessary(me.Id);
            var friend = me.Friends.SingleOrDefault(f => Session.Load<Person>(f.PersonId).EmailAddress == Email);
            if (friend == null)
                me.Friends.Add(new Friend { PersonId = person.Id });
            Session.SaveChanges();
        }

        private Person CreatePersonDocumentIfNecessary(string currentPersonId)
        {
            var person = Query(new PersonByEmail { Email = Email });
            if (person == null)
            {
                person = new Person
                    {
                        Id = Guid.NewGuid().ToString(),
                        EmailAddress = Email,
                        Name = Name,
                        Friends = new List<Friend>
                            {
                                new Friend
                                    {
                                        PersonId = currentPersonId,
                                        Rating = 0
                                    }
                            }
                    };
                Session.Store(person);
            }
            return person;
        }
    }
}