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
                return View(EventModelFromEvent(ev, GetCurrentUser()));
            }
            return View(new EventModel{ CreatedBy = GetCurrentUserId()});
        }

        private EventModel EventModelFromEvent(Event ev, User currentUser = null)
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
                               let user = RavenSession.Load<User>(i.UserId)
                               let friend = GetFriendFromUser(currentUser, i.UserId)
                               select new InvitationModel
                                   {
                                       Email = user.Username,
                                       UserId = user.Id,
                                       Name = user.Name,
                                       Rating = friend == null ? 0 : friend.Rating,
                                       EmailSent = i.EmailSent
                                   }).ToList()
                };
        }

        private Friend GetFriendFromUser(User user, string friendUserId)
        {
            if (user == null)
                return null;
            return user.Friends.SingleOrDefault(f => f.UserId == friendUserId);
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
                                                              select i.UserId));

            foreach (var invitee in invitees.Where(i => !i.EmailSent))
            {
                _emailer.Invite(GetCurrentUser(), RavenSession.Load<User>(invitee.UserId), ev);
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
            GetCurrentUser().Friends.Clear();
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public virtual PartialViewResult AddFriend(string name, string email)
        {
            var user = AddNewUserIfNecessary(name, email);

            var me = RavenSession.Query<User>()
                        .Include<User>(u => u.Friends.Select(f => f.UserId))
                        .SingleOrDefault(u => u.Username == User.Identity.Name);

            var friend = me.Friends.SingleOrDefault(f => RavenSession.Load<User>(f.UserId).Username == email);
            if (friend == null)
                me.Friends.Add(new Friend {UserId = user.Id});

            RavenSession.SaveChanges();
            return FirstPageOfFriends();
        }
        
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public virtual void RateFriend(string id, string rating)
        {
            var me = RavenSession.Query<User>()
                        .SingleOrDefault(u => u.Username == User.Identity.Name);

            var friend = me.Friends.SingleOrDefault(f => f.UserId == id);
            if (friend != null)
            {
                friend.Rating = Convert.ToInt32(rating);
            }
        }

        private User AddNewUserIfNecessary(string name, string email)
        {
            var user = RavenSession.Query<User>()
                                   .SingleOrDefault(u => u.Username == email);

            if (user == null)
            {
                user = new User
                    {
                        Id = Guid.NewGuid().ToString(),
                        Username = email,
                        Name = name,
                        Registered = false
                    };
                RavenSession.Store(user);
            }
            return user;
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
            GetCurrentUser().Friends.RemoveAll(f => f.UserId == id);
        }

        private IEnumerable<UserFriend> FriendsInternal(int pageIndex, int pageSize, out int totalPages, string friendSearch, bool unratedFriends)
        {
            return UsersFromFriends(GetCurrentUserId(), pageIndex, pageSize, out totalPages, friendSearch, unratedFriends);
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
            var matches = UsersFromFriends(GetCurrentUserId(), 0, 100, out totalPages, query, false);
            var dict = new Dictionary<string, object>();
            foreach (var match in matches.Where(match => !dict.ContainsKey(match.Email)))
                dict.Add(match.Email, new {
                    userId = match.UserId,
                    rating = match.Rating
                });
            return Json(dict,  JsonRequestBehavior.AllowGet);
        }
    }
}
