using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using Raven.Client;
using geeks.Models;

namespace geeks.Controllers
{
    public class HomeController : RavenController
    {
        public HomeController(IDocumentStore store) : base(store)
        {
        }

        public virtual ActionResult Index()
        {
            return View();
        }

        public virtual ActionResult About()
        {
            return View();
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public ActionResult SearchFriends(string friendSearch)
        {
            return RedirectToAction("Friends", new { friendSearch });
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public PartialViewResult AddFriendToEvent(string eventId, string userId)
        {
            var ev = RavenSession.Include<Event>(e => e.InviteeIds).Load<Event>(eventId);
            var inv = new List<string>(ev.InviteeIds);
            inv.Add(userId);
            ev.InviteeIds = inv;
            RavenSession.Store(ev);
            return PartialView("Invitees", EventModelFromEvent(ev));
        }

        [Authorize]
        public virtual ActionResult Event(string id)
        {
            if (!string.IsNullOrEmpty(id))
            {
                var ev = RavenSession.Include<Event>(e => e.InviteeIds).Load<Event>(id);
                return View(EventModelFromEvent(ev));
            }
            return View(new EventModel{ CreatedBy = GetCurrentUserId()});
        }

        private EventModel EventModelFromEvent(Event ev)
        {
            return new EventModel
                {
                    Id = ev.Id,
                    CreatedBy = ev.CreatedBy,
                    Date = ev.Date,
                    Description = ev.Description,
                    Title = ev.Title,
                    Venue = ev.Venue,
                    Invitees = (from i in ev.InviteeIds
                               let user = RavenSession.Load<User>(i)
                               select new UserFriend
                                   {
                                       Email = user.Username,
                                       UserId = user.Id,
                                       Name = user.Name
                                   }).ToList()
                };
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public virtual ActionResult Event(EventModel model)
        {
            if (ModelState.IsValid)
            {
                //save the event
                if (string.IsNullOrEmpty(model.Id))
                {
                    model.CreatedBy = GetCurrentUserId();
                }
                RavenSession.Store(new Event(model));
                return RedirectToAction("Events");
            }
            return View();
        }

        [Authorize]
        public virtual ActionResult Events()
        {
            var evs = RavenSession.Query<Event>()
                                  .Include<Event>(e => e.InviteeIds)
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
        public virtual PartialViewResult DeleteAllFriends(string id)
        {
            GetCurrentUser().Friends.Clear();
            RavenSession.SaveChanges();
            return FirstPageOfFriends();
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
            var friends = FriendsInternal(0, 10, out total, null);
            ViewBag.NumberOfPages = total;
            return PartialView("FriendsTable", friends);
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public virtual PartialViewResult DeleteFriend(string id)
        {
            GetCurrentUser().Friends.RemoveAll(f => f.UserId == id);
            RavenSession.SaveChanges();
            return FirstPageOfFriends();
        }

        private IEnumerable<UserFriend> FriendsInternal(int pageIndex, int pageSize, out int totalPages, string friendSearch)
        {
            return UsersFromFriends(GetCurrentUserId(), pageIndex, pageSize, out totalPages, friendSearch);
        }

        [Authorize]
        public virtual ActionResult Friends(int pageIndex = 0, int pageSize = 10, string friendSearch = null)
        {
            ViewBag.PageIndex = pageIndex;
            int total = 0;
            var friends = FriendsInternal(pageIndex, pageSize, out total, friendSearch);
            ViewBag.NumberOfPages = total;
            ViewBag.SearchTerm = friendSearch;
            return View(friends);
        }

        [Authorize]
        public JsonResult LookupFriends(string query)
        {
            var totalPages = 0;
            var matches = UsersFromFriends(GetCurrentUserId(), 0, 100, out totalPages, query);
            var dict = new Dictionary<string, string>();
            foreach (var match in matches.Where(match => !dict.ContainsKey(match.Email)))
                dict.Add(match.Email, match.UserId);
            return Json(dict,  JsonRequestBehavior.AllowGet);
        }
    }
}
