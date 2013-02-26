using System.Web.Mvc;

namespace geeks.Controllers
{
    public class ImportController : RavenController
    {
        [Authorize]
        public ActionResult ImportGoogle()
        {
            ViewBag.ImportFrom = "Google";
            return View("Import");
        }
        
        [Authorize]
        public ActionResult ImportFacebook()
        {
            ViewBag.ImportFrom = "Facebook";
            return View("Import");
        }
        
        [Authorize]
        public ActionResult ImportTwitter()
        {
            ViewBag.ImportFrom = "Twitter";
            return View("Import");
        }
    }
}