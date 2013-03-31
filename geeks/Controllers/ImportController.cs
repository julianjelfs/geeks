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

            var query = new ContactsQuery(ContactsQuery.CreateContactsUri("default"));
            query.NumberToRetrieve = 1000;
            ContactsFeed contacts = service.Query(query);
            ViewBag.ImportFrom = "Google";
            var userId = GetCurrentUserId();

            DeletePreviousImportIfNecessary(userId);

            var googleContact = new GoogleContact
                {
                    UserId = userId,
                    Contacts = (from ContactEntry entry in contacts.Entries
                                from email in entry.Emails
                                where entry.Name != null
                                where email != null
                                select new ImportModel
                                    {
                                        Import = false,
                                        EmailAddress = email.Address,
                                        Name = entry.Name.FullName
                                    }).ToList()
                };


            RavenSession.Store(googleContact);

            return View("Import");
        }

        public ViewResult Import()
        {
            return View();
        }

        [Authorize]
        public virtual JsonResult ImportData(int pageIndex = 0, int pageSize = 10, string search = null)
        {
            var contact = RavenSession.Query<GoogleContact>().FirstOrDefault(c => c.UserId == GetCurrentUserId());

            int total = 0;
            var contacts = GetImportModels(contact, out total, pageIndex, pageSize, search);
            return Json(new
            {
                Contacts = contacts,
                NumberOfPages = total,
                SearchTerm = search,
                PageIndex = pageIndex
            }, JsonRequestBehavior.AllowGet);
        }

        private List<ImportModel> GetImportModels(GoogleContact contact,
            out int totalPages,
            int pageIndex = 0, 
            int pageSize = 10, 
            string search = null)
        {
            var result = (from im in contact.Contacts
                          where (string.IsNullOrEmpty(search)
                              || (im.Name != null && im.Name.Contains(search))
                              || (im.EmailAddress != null && im.EmailAddress.Contains(search)))
                          select im).OrderBy(friend => friend.Name);

            totalPages = (int)Math.Ceiling((double)result.Count() / pageSize);
            return result.Skip(pageIndex * pageSize).Take(pageSize).ToList();
        } 

        private void DeletePreviousImportIfNecessary(string userId)
        {
            var contacts = RavenSession.Query<GoogleContact>().FirstOrDefault(c => c.UserId == userId);
            if(contacts != null)
                RavenSession.Delete(contacts);
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
        [ValidateAntiForgeryToken]
        public void ImportSelected(List<string> contacts)
        {
            var user = GetCurrentUser();
            var gc = RavenSession.Query<GoogleContact>().FirstOrDefault(c => c.UserId == user.Id);
            ImportContacts(gc, gc.Contacts.Where(model => contacts.Contains(model.EmailAddress)).ToList());
        }

        private void ImportContacts(GoogleContact gc, List<ImportModel> contacts)
        {
            var user = GetCurrentUser();
            var emails = new HashSet<string>(from m in contacts.Distinct() select m.EmailAddress);
            var users = RavenSession.Query<User>()
                            .Where(u => u.Username.In(emails))
                                    .ToDictionary(u => u.Username);

            using (var bulkInsert = Store.BulkInsert())
            {
                foreach (var importModel in contacts)
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

                user.Friends = user.Friends.Union(from i in contacts
                                                  where !user.Friends.Any(f => f.UserId == users[i.EmailAddress].Id)
                                                  select new Friend
                                                  {
                                                      UserId = users[i.EmailAddress].Id
                                                  }).ToList();
            }
            RavenSession.Delete(gc);
        }

        [Authorize]
        [ValidateAntiForgeryToken]
        public void ImportAll()
        {
            var user = GetCurrentUser();
            var gc = RavenSession.Query<GoogleContact>().FirstOrDefault(c => c.UserId == user.Id);
            var contacts = gc.Contacts.Where(m => m.EmailAddress != User.Identity.Name).ToList();
            ImportContacts(gc, contacts);
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