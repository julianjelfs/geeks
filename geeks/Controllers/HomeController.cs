using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web.Mvc;
using Raven.Client;
using geeks.Commands;
using geeks.Models;
using geeks.Queries;
using geeks.Services;

namespace geeks.Controllers
{
    [ValidateJsonAntiForgeryToken]
    public class HomeController : RavenController
    {
        private readonly IEmailer _emailer;

        public HomeController(IDocumentStore store, IEmailer emailer) : base(store)
        {
            _emailer = emailer;
        }

        public virtual ActionResult Index()
        {
            return View();
        }

        public virtual ActionResult About()
        {
            return View();
        }

        public virtual ActionResult Calendar()
        {
            return View();
        }

        [HttpPost]
        [Authorize]
        public ActionResult SearchFriends(string friendSearch, bool unratedFriends)
        {
            return RedirectToAction("Friends", new {friendSearch, unratedFriends});
        }

        [Authorize]
        public virtual ActionResult Event(string id, string userId)
        {
            ViewBag.EventId = id;
            return View();
        }

        [Authorize]
        public virtual JsonNetResult EventData(string id, string userId)
        {
            if (!string.IsNullOrEmpty(id))
            {
                var me = Query(new PersonByUserId {UserId = GetCurrentUserId()});
                var ev = Query(new EventWithInvitations { EventId = id });
                return JsonNet(EventModelFromEvent(ev, me));
            }
            return JsonNet(new EventModel {CreatedBy = GetCurrentUserId()});
        }


        [HttpPost]
        [Authorize]
        public virtual HttpStatusCodeResult SaveEvent(EventModel model)
        {
            if (ModelState.IsValid)
            {
                Command(new SaveEventCommand
                    {
                        Event = new Event(model),
                        Emailer = _emailer
                    });
            }
            return new HttpStatusCodeResult(HttpStatusCode.OK);
        }

        private EventModel EventModelFromEvent(Event ev, Person currentPerson = null)
        {
            return new EventModel
                {
                    Id = ev.Id,
                    CreatedByUserName = RavenSession.Load<User>(ev.CreatedBy).Username,
                    CreatedBy = ev.CreatedBy,
                    Date = ev.Date,
                    Description = ev.Description,
                    Latitude = ev.Latitude,
                    Longitude = ev.Longitude,
                    Venue = ev.Venue,
                    Invitations = (from i in ev.Invitations
                                   let person = RavenSession.Load<Person>(i.PersonId)
                                   let friend = GetFriendFromPerson(currentPerson, i.PersonId)
                                   select new InvitationModel
                                       {
                                           Email = person.EmailAddress,
                                           PersonId = person.Id,
                                           Rating = friend == null ? 0 : friend.Rating,
                                           EmailSent = i.EmailSent
                                       }).ToList()
                };
        }

        private Friend GetFriendFromPerson(Person person, string friendPersonId)
        {
            if (person == null)
                return null;
            return person.Friends.SingleOrDefault(f => f.PersonId == friendPersonId);
        }

        [Authorize]
        public virtual JsonNetResult EventsData(int pageIndex = 0, int pageSize = 10, string search = null)
        {
            var person = Query(new PersonByUserId { UserId = GetCurrentUserId() });
            var result = Query(new EventsDataForUser
                {
                    Search = search,
                    PageIndex = pageIndex,
                    PageSize = pageSize,
                    CurrentPerson = person
                });

            return JsonNet(new
                {
                    Events = from ev in result.List
                                 select EventModelFromEvent(ev, person),
                    NumberOfPages = result.TotalPages,
                    SearchTerm = search,
                    PageIndex = pageIndex
                });
        }

        [Authorize]
        public virtual ActionResult Events()
        {
            return View();
        }

        [HttpPost]
        [Authorize]
        public void DeleteEvent(string id)
        {
            Command(new DeleteEventCommand{ EventId = id });
        }

        [HttpPost]
        [Authorize]
        public virtual void DeleteAllFriends(string id)
        {
            Command(new DeleteAllFriendsCommand());
        }

        [HttpPost]
        [Authorize]
        public virtual void AddFriend(string name, string email)
        {
            Command(new AddFriendCommand
            {
                Email = email,
                Name = name
            });
        }

        [HttpPost]
        [Authorize]
        public virtual void RateFriend(string id, string rating)
        {
            Command(new RateFriendCommand
            {
                PersonId = id,
                Rating = Convert.ToInt32(rating)
            });
        }

        [HttpPost]
        [Authorize]
        [ValidateJsonAntiForgeryToken]
        public virtual void DeleteFriend(string id)
        {
            Command(new DeleteFriendCommand { PersonId = id });
        }

        [Authorize]
        public virtual JsonNetResult FriendsData(int pageIndex = 0, int pageSize = 10, string friendSearch = null,
                                                 bool unratedFriends = false)
        {
            var friends = Query(new FriendsForAPerson
            {
                PersonId = GetCurrentPersonId(),
                Search = friendSearch,
                PageIndex = pageIndex,
                PageSize = pageSize,
                Unrated = unratedFriends
            });

            return JsonNet(new
                {
                    Friends = friends.List,
                    NumberOfPages = friends.TotalPages,
                    SearchTerm = friendSearch,
                    Unrated = unratedFriends,
                    PageIndex = pageIndex
                });
        }

        [Authorize]
        public ViewResult Friends()
        {
            return View();
        }

        [Authorize]
        public JsonNetResult LookupFriends(string query)
        {
            var matches = Query(new FriendsForAPerson
                {
                    PersonId = GetCurrentPersonId(),
                    Search = query,
                    PageIndex = 0,
                    PageSize = 100,
                    Unrated = false
                });

            var dict = new Dictionary<string, object>();
            foreach (var match in matches.List.Where(match => !dict.ContainsKey(match.Email)))
                dict.Add(match.Email, new
                    {
                        match.PersonId,
                        match.Rating
                    });
            return JsonNet(dict);
        }
    }
}