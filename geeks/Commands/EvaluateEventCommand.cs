using geeks.Models;
using System.Linq;
using geeks.Queries;

namespace geeks.Commands
{
    /// <summary>
    /// works out the current score for this event as it stands at the moment
    /// </summary>
    public class EvaluateEventCommand : Command
    {
        public Event Event { get; set; }

        public override void Execute()
        {
            var score = 0D;

            //this is actually wrong at the moment because it doesn't take into 
            //account whether each invitee is actually coming or not
            foreach (var i1 in Event.Invitations)
            {
                var currentPerson = Query(new PersonByIdWithFriends {Id = i1.PersonId});
                foreach (var i2 in Event.Invitations)
                {
                    if (i2.PersonId == i1.PersonId) continue;

                    var f = currentPerson.Friends.SingleOrDefault(friend => friend.PersonId == i2.PersonId);
                    if (f != null)
                    {
                        score += f.Rating;
                    }
                }
            }

            Event.Score = score;
        }
    }
}