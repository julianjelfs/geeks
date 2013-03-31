using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using Raven.Client;
using geeks.Models;
using geeks.Services;
using Raven.Client.Linq;

namespace geeks.Controllers
{
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
        [ValidateAntiForgeryToken]
        public ActionResult SearchFriends(string friendSearch, bool unratedFriends)
        {
            return RedirectToAction("Friends", new { friendSearch, unratedFriends });
        }

        [Authorize]
        public virtual ActionResult Event(string id, string userId)
        {
            if (!string.IsNullOrEmpty(id))
            {
                var ev = RavenSession.Include<Event>(e => e.Invitations).Load<Event>(id);
                return View(EventModelFromEvent(ev, GetCurrentPerson()));
            }
            return View(new EventModel{ CreatedBy = GetCurrentUserId()});
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
                    Title = ev.Title,
                    Latitude = ev.Latitude,
                    Longitude = ev.Longitude,
                    Venue = ev.Venue,
                    Invitations = (from i in ev.Invitations
                               let user = RavenSession.Load<User>(i.PersonId)
                               let friend = GetFriendFromPerson(currentPerson, i.PersonId)
                               select new InvitationModel
                                   {
                                       Email = user.Username,
                                       PersonId = user.Id,
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

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public virtual ActionResult Event(EventModel model)
        {
            if (ModelState.IsValid)
            {
                model.CreatedBy = GetCurrentUserId();
                RavenSession.Store(SendEmailToInvitees(new Event(model)));
                return RedirectToAction("Events");
            }
            return View(EventModelFromEvent(new Event(model)));
        }

        private Event SendEmailToInvitees(Event ev)
        {
            var invitees = ev.Invitations.ToArray();
            var users = RavenSession.Query<User>()
                                    .Where(u => u.Username.In(from i in invitees
                                                              where !i.EmailSent
                                                              select i.PersonId));

            foreach (var invitee in invitees.Where(i => !i.EmailSent))
            {
                _emailer.Invite(GetCurrentPerson(), RavenSession.Load<Person>(invitee.PersonId), ev);
                invitee.EmailSent = true;
            }
            ev.Invitations = invitees;
            return ev;
        }

        [Authorize]
        public virtual ActionResult Events()
        {
            var evs = RavenSession.Query<Event>()
                                  .Include<Event>(e => e.Invitations)
                                  .Include<Event>(e => e.CreatedBy)
                                  .Where(e => e.CreatedBy == GetCurrentUserId())
                                  .ToList();
            var models = (from e in evs select EventModelFromEvent(e)).ToList();
            return View(models);
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public void DeleteEvent(int id)
        {
            var model = RavenSession.Load<EventModel>(id);
            if (model != null)
            {
                RavenSession.Delete(model);
            }
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public virtual void DeleteAllFriends(string id)
        {
            GetCurrentPerson().Friends.Clear();
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public virtual void AddFriend(string name, string email)
        {
            var person = CreatePersonDocumentIfNecessary(name, email);

            var me = RavenSession.Query<Person>()
                        .Include<Person>(p => p.Friends.Select(f => f.PersonId))
                        .SingleOrDefault(p => p.EmailAddress == User.Identity.Name);

            var friend = me.Friends.SingleOrDefault(f => RavenSession.Load<Person>(f.PersonId).EmailAddress == email);
            if (friend == null)
                me.Friends.Add(new Friend {PersonId = person.Id});

            RavenSession.SaveChanges();
        }
        
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public virtual void RateFriend(string id, string rating)
        {
            var me = RavenSession.Query<Person>()
                        .FirstOrDefault(p => p.EmailAddress == User.Identity.Name);

            var friend = me.Friends.SingleOrDefault(f => f.PersonId == id);
            if (friend != null)
            {
                friend.Rating = Convert.ToInt32(rating);
            }
        }

        private Person CreatePersonDocumentIfNecessary(string name, string email)
        {
            var person = RavenSession.Query<Person>()
                                   .FirstOrDefault(p => p.EmailAddress == email);

            if (person == null)
            {
                person = new Person
                    {
                        Id = Guid.NewGuid().ToString(),
                        EmailAddress = email,
                        Name = name
                    };
                RavenSession.Store(person);
            }
            return person;
        }

        private PartialViewResult FirstPageOfFriends()
        {
            ViewBag.PageIndex = 0;
            int total = 0;
            var friends = FriendsInternal(0, 10, out total, null, false);
            ViewBag.NumberOfPages = total;
            return PartialView("FriendsTable", friends);
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public virtual void DeleteFriend(string id)
        {
            GetCurrentPerson().Friends.RemoveAll(f => f.PersonId == id);
        }

        private IEnumerable<PersonFriend> FriendsInternal(int pageIndex, int pageSize, out int totalPages, string friendSearch, bool unratedFriends)
        {
            return PersonsFromFriends(GetCurrentPersonId(), pageIndex, pageSize, out totalPages, friendSearch, unratedFriends);
        }

        [Authorize]
        public virtual JsonResult FriendsData(int pageIndex = 0, int pageSize = 10, string friendSearch = null, bool unratedFriends = false)
        {
            int total = 0;
            var friends = FriendsInternal(pageIndex, pageSize, out total, friendSearch, unratedFriends);
            return Json(new
            {
                Friends = friends,
                NumberOfPages = total,
                SearchTerm = friendSearch,
                Unrated = unratedFriends,
                PageIndex = pageIndex
            }, JsonRequestBehavior.AllowGet);
        }

        [Authorize]
        public ViewResult Friends()
        {
            return View();
        }

        [Authorize]
        public JsonResult LookupFriends(string query)
        {
            var totalPages = 0;
            var matches = PersonsFromFriends(GetCurrentPersonId(), 0, 100, out totalPages, query, false);
            var dict = new Dictionary<string, object>();
            foreach (var match in matches.Where(match => !dict.ContainsKey(match.Email)))
                dict.Add(match.Email, new {
                    personId = match.PersonId,
                    rating = match.Rating
                });
            return Json(dict,  JsonRequestBehavior.AllowGet);
        }
    }
}
