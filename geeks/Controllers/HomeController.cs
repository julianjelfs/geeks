using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
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
                RavenSession.Store(model);
                return RedirectToAction("Events");
            }
            return View();
        }
        
        [Authorize]
        public ActionResult DeleteEvent(int id)
        {
            var model = RavenSession.Load<EventModel>(id);
            if (model != null)
            {
                RavenSession.Delete(model);
            }
            return RedirectToAction("Events");
        }

        [Authorize]
        public ActionResult Events()
        {
            return View(from ev in RavenSession.Query<EventModel>()
                        select ev);
        }
    }
}
