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

        [Authorize]
        public virtual ActionResult Event(int id)
        {
            if (id > 0)
            {
                return View(RavenSession.Load<EventModel>(id));
            }
            return View();
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public virtual ActionResult Event(EventModel model)
        {
            if (ModelState.IsValid)
            {
                //save the event
                if (model.Id == 0)
                {
                    model.CreatedBy = User.Identity.Name;
                }
                RavenSession.Store(model);
                return RedirectToAction("Events");
            }
            return View();
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
        public virtual PartialViewResult DeleteFriend(string id, int pageIndex = 0, int pageSize = 10)
        {
            GetCurrentUser().Friends.RemoveAll(f => f.UserId == id);
            RavenSession.SaveChanges();
            ViewBag.PageIndex = pageIndex;
            int total = 0;
            var friends = FriendsInternal(pageIndex, pageSize, out total);
            ViewBag.NumberOfPages = total;
            return PartialView("FriendsTable", friends);
        }

        private IEnumerable<UserFriend> FriendsInternal(int pageIndex, int pageSize, out int totalPages)
        {
            return UsersFromFriends(GetCurrentUserId(), pageIndex, pageSize, out totalPages);
        }
        
        [Authorize]
        public virtual ActionResult Friends(int pageIndex = 0, int pageSize = 10)
        {
            ViewBag.PageIndex = pageIndex;
            int total = 0;
            var friends = FriendsInternal(pageIndex, pageSize, out total);
            ViewBag.NumberOfPages = total;
            return View(friends);
        }

        [Authorize]
        public virtual ActionResult Events()
        {
            return View(from ev in RavenSession.Query<EventModel>()
                        where ev.CreatedBy == User.Identity.Name
                        select ev);
        }
    }
}
