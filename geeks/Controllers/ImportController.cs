using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Web.Mvc;
using DotNetOpenAuth.Messaging;
using DotNetOpenAuth.OAuth2;
using Google.GData.Client;
using Google.GData.Contacts;
using Raven.Client;
using geeks.Models;
using Raven.Client.Linq;

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

        public ImportController(IDocumentStore store) : base(store)
        {
        }

        [AllowAnonymous]
        public virtual ActionResult GoogleAuthCallback(string returnUrl)
        {
            return Redirect(returnUrl);
        }

        public virtual ActionResult OAuth()
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
            OutgoingWebResponse r = client.PrepareRequestUserAuthorization(state);
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
            IAuthorizationState auth = client.ProcessUserAuthorization(Request);
            Session["auth"] = auth;
            var authFactory = new GAuthSubRequestFactory("cp", "Geeks Dilemma") {Token = auth.AccessToken};
            var service = new ContactsService(authFactory.ApplicationName) {RequestFactory = authFactory};

            //var settings = new RequestSettings("<var>Geeks Dilemma</var>", auth.AccessToken);
            //var cr = new ContactsRequest(settings);
            var query = new ContactsQuery(ContactsQuery.CreateContactsUri("default"));
            query.NumberToRetrieve = 1000;
            ContactsFeed contacts = service.Query(query);
            ViewBag.ImportFrom = "Google";

            return View("Import", (from ContactEntry entry in contacts.Entries
                                   from email in entry.Emails
                                   where entry.Name != null
                                   where email != null
                                   select new ImportModel
                                       {
                                           Import = false,
                                           EmailAddress = email.Address,
                                           Name = entry.Name.FullName
                                       }).ToList());
        }

        /*
         * This one is on hold for now
         * https://trello.com/card/importing-contacts-from-facebook/5128e013fff24191630070f2/13
         * 
        [Authorize]
        public ActionResult ImportFacebook()
        {
            if (Session["facebookToken"] != null)
            {
                var client = new FacebookClient(Session["facebookToken"] as string);
                dynamic me = client.Get("me/friends");
            }
            ViewBag.ImportFrom = "Facebook";
            return View("Import", new List<ImportModel>());  
        }
         */

        [Authorize]
        public virtual ActionResult ImportTwitter()
        {
            ViewBag.ImportFrom = "Twitter";
            return View("Import");
        }

        [Authorize]
        [ValidateAntiForgeryToken]
        public virtual ActionResult PerformImport(List<ImportModel> model)
        {
            var user = GetCurrentUser();
            
            model = model.Where(m => m.EmailAddress != User.Identity.Name
                                     && m.Import).ToList();

            var emails = new HashSet<string>(from m in model.Distinct() select m.EmailAddress);
            var users = RavenSession.Query<User>()
                            .Where(u => u.Username.In(emails))
                                    .ToDictionary(u => u.Username);

            using (var bulkInsert = Store.BulkInsert())
            {
                foreach (var importModel in model)
                {
                    if (importModel.EmailAddress == User.Identity.Name) continue;
                    if (users.ContainsKey(importModel.EmailAddress)) continue;

                    var newUser = new User
                        {
                            Username = importModel.EmailAddress,
                            Name = importModel.Name,
                            Id = Guid.NewGuid().ToString()
                        };
                    bulkInsert.Store(newUser);
                    users.Add(newUser.Username, newUser);
                }

                user.Friends = user.Friends.Union(from i in model
                                                  where !user.Friends.Any(f => f.UserId == users[i.EmailAddress].Id)
                                                  select new Friend
                                                      {
                                                          UserId = users[i.EmailAddress].Id
                                                      }).ToList();
            }

            return RedirectToAction("Friends", "Home");
        }
    }
}