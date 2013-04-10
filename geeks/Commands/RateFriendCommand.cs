using System;
using System.Linq;
using Raven.Client;
using geeks.Exceptions;
using geeks.Models;
using geeks.Queries;

namespace geeks.Commands
{
    public class RateFriendCommand : Command
    {
        public string PersonId { get; set; }
        public int Rating { get; set; }

        public override void Execute()
        {
            var me = Query(new PersonByUserIdWithFriends {UserId = CurrentUserId});
            if (me.Id == PersonId)
                throw new CantRankYourselfException();

            var friend = me.Friends.SingleOrDefault(f => f.PersonId == PersonId);
            if (friend == null)
            {
                //this person is not my friend yet 
                me.Friends.Add(new Friend
                    {
                        PersonId = PersonId,
                        Rating = Convert.ToInt32(Rating)
                    });
            }
            else
            {
                friend.Rating = Convert.ToInt32(Rating);
            }
        }
    }
}