using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using Raven.Client;
using geeks.Models;

namespace geeks.Controllers
{
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
            string friendSearch)
        {
            var user = RavenSession.Include<User>(u=>u.Friends.Select(f=>f.UserId))
                       .Load<User>(userId);

            var result = (from f in user.Friends
                            let u = RavenSession.Load<User>(f.UserId)
                            where string.IsNullOrEmpty(friendSearch)
                                || u.Username.Contains(friendSearch)
                                || u.Name.Contains(friendSearch)
                            select new UserFriend
                                {
                                    UserId = u.Id, Name = u.Name, Email = u.Username, Rating = f.Rating
                                }).OrderBy(friend => friend.Name);

            totalPages = (int)Math.Ceiling((double)result.Count() / pageSize);

            return result.Skip(pageIndex*pageSize).Take(pageSize);
        }
    }
}