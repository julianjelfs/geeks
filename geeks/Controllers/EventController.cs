using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using Raven.Client;
using geeks.Models;

namespace geeks.Controllers
{
    public class EventController : RavenApiController
    {
        public IEnumerable<EventModel> Get()
        {
            var evs = RavenSession.Query<Event>()
                                  .Include<Event>(e => e.Invitations)
                                  .Include<Event>(e => e.CreatedBy)
//                                  .Where(e => e.Invitations.Any(i => i.UserId == GetCurrentUserId()))
                                  .ToList();

             return (from e in evs select EventModelFromEvent(e)).ToList();
        }

        private EventModel EventModelFromEvent(Event ev, User currentUser = null)
        {
            return new EventModel
            {
                Id = ev.Id,
                CreatedByUserName = RavenSession.Load<User>(ev.CreatedBy).Username,
                CreatedBy = ev.CreatedBy,
                MyEvent = currentUser != null && ev.CreatedBy == currentUser.Id,
                Date = ev.Date,
                Description = ev.Description,
                Latitude = ev.Latitude,
                Longitude = ev.Longitude,
                Venue = ev.Venue,
                Invitations = (from i in ev.Invitations
                               let user = RavenSession.Load<User>(i.UserId)
                               let friend = GetFriendFromUser(currentUser, i.UserId)
                               select new InvitationModel
                               {
                                   Email = user.Username,
                                   UserId = user.Id,
                                   Name = user.Name,
                                   Rating = friend == null ? 0 : friend.Rating,
                                   EmailSent = i.EmailSent,
                                   CanRate = currentUser == null || currentUser.Id != user.Id
                               }).ToList()
            };
        }

        private Friend GetFriendFromUser(User user, string friendUserId)
        {
            if (user == null)
                return null;
            return user.Friends.SingleOrDefault(f => f.UserId == friendUserId);
        }

        // GET api/event/5
        public string Get(int id)
        {
            return "value";
        }

        // POST api/event
        public void Post([FromBody]string value)
        {
        }

        // PUT api/event/5
        public void Put(int id, [FromBody]string value)
        {
        }

        // DELETE api/event/5
        public void Delete(int id)
        {
        }
    }
}
