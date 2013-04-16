using System.Collections.Generic;
using geeks.Models;
using System.Linq;
using geeks.Queries;
using geeks.Services;

namespace geeks.Commands
{
    /// <summary>
    /// works out the current score for this event as it stands at the moment
    /// </summary>
    public class ScoreEventCommand : Command
    {
        private const double _threshold = 75;

        public Event Event { get; set; }
        public IEmailer Emailer { get; set; }

        public override void Execute()
        {
            var everyoneComingScore = 0D;
            var maxScore = Event.Invitations.Count() * 10 * 2;
            var actualScore = 0D;
            var previousScore = Event.PercentageScore();
            var organiser = Query(new PersonByUserId { UserId = Event.CreatedBy });
            var invitees = new List<Person>();

            foreach (var i1 in Event.Invitations)
            {
                var currentPerson = Query(new PersonByIdWithFriends {Id = i1.PersonId});
                invitees.Add(currentPerson);
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
            var newScore = Event.PercentageScore();

            if (previousScore < _threshold && newScore > _threshold)
            {
                Emailer.EventIsOn(Event, organiser, invitees);
            }
            
            if (previousScore > _threshold && newScore < _threshold)
            {
                Emailer.EventIsOff(Event, organiser, invitees);
            }
        }
    }
}