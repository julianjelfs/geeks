using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using WebMatrix.WebData;
using geeks.Models;

namespace geeks.Controllers
{
    public class HomeController : RavenController
    {
        public ActionResult Index()
        {
            return View();
        }

        public ActionResult About()
        {
            return View();
        }

        [Authorize]
        public ActionResult Event(int id)
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
        public ActionResult Event(EventModel model)
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
        public PartialViewResult DeleteFriend(string id)
        {
            var user = (from u in RavenSession.Query<UserModel>()
                        where u.UserName == User.Identity.Name
                        select u).FirstOrDefault();
            if (user != null)
            {
                user.Friends.RemoveAll(f => f.Email == id);
                RavenSession.Store(user);
            }
            return PartialView("FriendsTable", FriendsInternal());
        }

        private IEnumerable<FriendModel> FriendsInternal()
        {
            var user = (from u in RavenSession.Query<UserModel>()
                        where u.UserName == User.Identity.Name
                        select u).FirstOrDefault();

            return user == null
                       ? new List<FriendModel>()
                       : user.Friends.OrderBy(f => f.Name).ToList();
        }
        
        [Authorize]
        public ActionResult Friends()
        {
            return View(FriendsInternal());
        }

        [Authorize]
        public ActionResult Events()
        {
            return View(from ev in RavenSession.Query<EventModel>()
                        where ev.CreatedBy == User.Identity.Name
                        select ev);
        }
    }
}
