using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace geeks.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            ViewBag.Message = "Organise a night out without all the game theory. Who is going? Who is not going? Will there be enough people I like? Why not relax and let the machines decide.";

            return View();
        }

        public ActionResult About()
        {
            ViewBag.Message = "Your app description page.";

            return View();
        }

        [Authorize]
        public ActionResult Start()
        {
            ViewBag.Message = "Create an event";

            return View();
        }
    }
}
