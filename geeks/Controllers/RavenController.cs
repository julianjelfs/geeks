using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using System.Web.Http.Controllers;
using System.Web.Mvc;
using Microsoft.Practices.ServiceLocation;
using Raven.Client;
using geeks.Models;

namespace geeks.Controllers
{
    public class RavenApiController : ApiController
    {
        public IDocumentSession RavenSession { get; set; }

        public override Task<HttpResponseMessage> ExecuteAsync(
            HttpControllerContext controllerContext, 
            CancellationToken cancellationToken)
        {
            RavenSession = ServiceLocator.Current.GetInstance<IDocumentStore>().OpenSession();
            return base.ExecuteAsync(controllerContext, cancellationToken)
                .ContinueWith(task =>
                    {
                        using (RavenSession)
                        {
                            if(task.Status != TaskStatus.Faulted && RavenSession != null)
                                RavenSession.SaveChanges();
                        }
                        return task;
                    }).Unwrap();
        }

        protected string GetCurrentUserId()
        {
            var user = RavenSession.Query<User>()
                            .SingleOrDefault(u => u.Username == User.Identity.Name);

            if (user == null)
            {
                throw new ApplicationException(string.Format("Unknown user {0}", User.Identity.Name));
            }
            return user.Id;
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

        protected User GetCurrentUser()
        {
            return RavenSession.Load<User>(GetCurrentUserId());
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

        protected IEnumerable<UserFriend> UsersFromFriends(string userId, 
            int pageIndex, 
            int pageSize, 
            out int totalPages,
            string friendSearch,
            bool unratedFriends)
        {
            var user = RavenSession.Include<User>(u=>u.Friends.Select(f=>f.UserId))
                       .Load<User>(userId);

            var result = (from f in user.Friends
                            let u = RavenSession.Load<User>(f.UserId)
                            where (string.IsNullOrEmpty(friendSearch)
                                || (u.Username != null && u.Username.Contains(friendSearch))
                                || (u.Name != null && u.Name.Contains(friendSearch)))
                                && (f.Rating == 0 || !unratedFriends)
                            select new UserFriend
                                {
                                    UserId = u.Id, Name = u.Name, Email = u.Username, Rating = f.Rating
                                }).OrderBy(friend => friend.Name);

            totalPages = (int)Math.Ceiling((double)result.Count() / pageSize);

            return result.Skip(pageIndex*pageSize).Take(pageSize);
        }
    }
}