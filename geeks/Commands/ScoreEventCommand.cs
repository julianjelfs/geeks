using geeks.Models;
using System.Linq;
using geeks.Queries;

namespace geeks.Commands
{
    /// <summary>
    /// works out the current score for this event as it stands at the moment
    /// </summary>
    public class ScoreEventCommand : Command
    {
        public Event Event { get; set; }

        public override void Execute()
        {
            var everyoneComingScore = 0D;
            var maxScore = Event.Invitations.Count() * 10 * 2;
            var actualScore = 0D;

            foreach (var i1 in Event.Invitations)
            {
                var currentPerson = Query(new PersonByIdWithFriends {Id = i1.PersonId});
                foreach (var i2 in Event.Invitations)
                {
                    if (i2.PersonId == i1.PersonId) continue;

                    var f = currentPerson.Friends.SingleOrDefault(friend => friend.PersonId == i2.PersonId);
                    if (f != null)
                    {
                        everyoneComingScore += f.Rating;
                        if (i1.Response == InvitationResponse.Yes
                            && i2.Response == InvitationResponse.Yes)
                        {
                            actualScore += f.Rating;
                        }
                    }
                }
            }

            Event.EveryoneComingScore = everyoneComingScore;
            Event.TheoreticalMaximumScore = maxScore;
            Event.Score = actualScore;
        }
    }
}