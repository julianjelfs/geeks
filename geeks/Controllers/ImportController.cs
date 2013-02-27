using System;
using System.Linq;
using System.Web.Mvc;
using DotNetOpenAuth.Messaging;
using DotNetOpenAuth.OAuth2;
using Google.Contacts;
using Google.GData.Client;
using Google.GData.Extensions;
using System.Collections.Generic;
using geeks.Models;

namespace geeks.Controllers
{
    public class AuthHelper
    {
        public static WebServerClient CreateClient()
        {
            AuthorizationServerDescription desc = GetAuthServerDescription();
            var client = new WebServerClient(desc, clientIdentifier: "475779814525.apps.googleusercontent.com");
            client.ClientCredentialApplicator = ClientCredentialApplicator.PostParameter("plCPGSN2a218q7gHYmy0-BW1");
            return client;
        }

        public static AuthorizationServerDescription GetAuthServerDescription()
        {
            var authServerDescription = new AuthorizationServerDescription();
            authServerDescription.AuthorizationEndpoint = new Uri(@"https://accounts.google.com/o/oauth2/auth");
            authServerDescription.TokenEndpoint = new Uri(@"https://accounts.google.com/o/oauth2/token");
            authServerDescription.ProtocolVersion = ProtocolVersion.V20;
            return authServerDescription;
        }
    }

    public class ImportController : RavenController
    {
        private static readonly WebServerClient client = AuthHelper.CreateClient();

        [AllowAnonymous]
        public ActionResult GoogleAuthCallback(string returnUrl)
        {
            return Redirect(returnUrl);
        }

        public ActionResult OAuth()
        {
            if (string.IsNullOrEmpty(Request.QueryString["code"]))
            {
                return InitAuth();
            }
            return OAuthCallback();
        }

        private ActionResult InitAuth()
        {
            var state = new AuthorizationState {Callback = new Uri(RemoveQueryStringFromUri(Request.Url.AbsoluteUri))};
            state.Scope.Add("https://www.google.com/m8/feeds");
            var r = client.PrepareRequestUserAuthorization(state);
            return r.AsActionResult();
        }

        private static string RemoveQueryStringFromUri(string uri)
        {
            int index = uri.IndexOf('?');
            if (index > -1)
            {
                uri = uri.Substring(0, index);
            }
            return uri;
        }

        private ActionResult OAuthCallback()
        {
            var auth = client.ProcessUserAuthorization(Request);
            Session["auth"] = auth;
            var settings = new RequestSettings("<var>Geeks Dilemma</var>", auth.AccessToken);
            var cr = new ContactsRequest(settings);
            var contacts = cr.GetContacts();
            ViewBag.ImportFrom = "Google";
            return View("Import", (from contact in contacts.Entries
                                   from email in contact.Emails
                                   select new ImportModel {
                                       Import = false,
                                       EmailAddress = email.Address,
                                       Name = contact.Name.FullName
                                   }).ToList());
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
        
        [Authorize]
        public ActionResult Friends()
        {
            var user = (from u in RavenSession.Query<UserModel>()
                        where u.UserName == User.Identity.Name
                        select u).FirstOrDefault();

            return View(user == null
                              ? new List<FriendModel>()
                              : user.Friends);
        }

        [Authorize]
        [ValidateAntiForgeryToken]
        public ActionResult PerformImport(List<ImportModel> model)
        {
            var user = (from u in RavenSession.Query<UserModel>()
                       where u.UserName == User.Identity.Name
                       select u).FirstOrDefault();
            user = user ?? new UserModel {UserName = User.Identity.Name};

            user.Friends = (from i in model
                            select new FriendModel {Name = i.Name, Email = i.EmailAddress}).ToList();

            RavenSession.Store(user);

            return View("Friends", user.Friends);
        }
    }
}