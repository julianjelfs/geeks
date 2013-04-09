using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Mvc;
using GravatarHelper;
using Newtonsoft.Json;
using Raven.Client;
using geeks.Models;

namespace geeks.Controllers
{
    public class JsonNetResult : ActionResult
    {
        public Encoding ContentEncoding { get; set; }
        public string ContentType { get; set; }
        public object Data { get; set; }

        public JsonSerializerSettings SerializerSettings { get; set; }
        public Formatting Formatting { get; set; }

        public JsonNetResult()
        {
            SerializerSettings = new JsonSerializerSettings();
        }

        public override void ExecuteResult(ControllerContext context)
        {
            if (context == null)
                throw new ArgumentNullException("context");

            var response = context.HttpContext.Response;

            response.ContentType = !string.IsNullOrEmpty(ContentType)
              ? ContentType
              : "application/json";

            if (ContentEncoding != null)
                response.ContentEncoding = ContentEncoding;

            if (Data != null)
            {
                var writer = new JsonTextWriter(response.Output) { Formatting = Formatting };

                var serializer = JsonSerializer.Create(SerializerSettings);
                serializer.Serialize(writer, Data);

                writer.Flush();
            }
        }
    }

    public class RavenController : Controller
    {
        protected IDocumentStore Store { get; private set; }
        protected IDocumentSession RavenSession { get; private set; }

        public RavenController(IDocumentStore store)
        {
            Store = store;
        }

        protected override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            RavenSession = Store.OpenSession();
        }

        protected string GetCurrentUserId()
        {
            if (Session["UserId"] == null)
            {
                var user = RavenSession.Query<User>()
                                .SingleOrDefault(u => u.Username == User.Identity.Name);

                if (user == null)
                {
                    throw new ApplicationException(string.Format("Unknown user {0}", User.Identity.Name));
                }
                Session["UserId"] = user.Id;
            }
            return Session["UserId"] as string;
        }
        
        protected string GetCurrentPersonId()
        {
            if (Session["PersonId"] == null)
            {
                var person = RavenSession.Query<Person>()
                                .FirstOrDefault(p => p.UserId == GetCurrentUserId());

                if (person == null)
                {
                    throw new ApplicationException(string.Format("Unknown person {0}", User.Identity.Name));
                }
                Session["PersonId"] = person.Id;
            }
            return Session["PersonId"] as string;
        }

        protected User GetCurrentUser()
        {
            return RavenSession.Load<User>(GetCurrentUserId());
        }
        
        protected Person GetCurrentPerson()
        {
            return RavenSession.Query<Person>()
                .FirstOrDefault(person => person.UserId == GetCurrentUserId());
        }

        protected override void OnActionExecuted(ActionExecutedContext filterContext)
        {
            if (filterContext.IsChildAction)
                return;

            using (RavenSession)
            {
                if (filterContext.Exception != null)
                    return;

                if (RavenSession != null)
                    RavenSession.SaveChanges();
            }
        }

        protected IEnumerable<PersonFriend> PersonsFromFriends(string personId, 
            int pageIndex, 
            int pageSize, 
            out int totalPages,
            string friendSearch,
            bool unratedFriends)
        {
            var person = RavenSession.Include<Person>(u=>u.Friends.Select(f=>f.PersonId))
                       .Load<Person>(personId);

            var result = (from f in person.Friends
                            let p = RavenSession.Load<Person>(f.PersonId)
                            where (string.IsNullOrEmpty(friendSearch)
                                || (p.EmailAddress != null && p.EmailAddress.Contains(friendSearch))
                                || (p.Name != null && p.Name.Contains(friendSearch)))
                                && (f.Rating == 0 || !unratedFriends)
                            select new PersonFriend
                                {
                                    PersonId = p.Id, 
                                    Name = p.Name, 
                                    Email = p.EmailAddress, 
                                    Rating = f.Rating,
                                    GravatarLink = GravatarHelper.GravatarHelper.CreateGravatarUrl(p.EmailAddress, 30, "http://dl.dropbox.com/u/26218407/logo-small.png", null, null, null)
                                }).OrderBy(friend => friend.Name);

            totalPages = (int)Math.Ceiling((double)result.Count() / pageSize);

            return result.Skip(pageIndex*pageSize).Take(pageSize);
        }

        protected JsonNetResult JsonNet(object data)
        {
            return new JsonNetResult
                {
                    Data = data
                };
        }
    }
}