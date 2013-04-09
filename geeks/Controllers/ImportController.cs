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

    [ValidateJsonAntiForgeryToken]
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
                                        EmailAddress = email.Address.ToLower(),
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
        public void ImportSelected(List<string> contacts)
        {
            var user = GetCurrentUser();
            var gc = RavenSession.Query<GoogleContact>().FirstOrDefault(c => c.UserId == user.Id);
            ImportContacts(gc, gc.Contacts.Where(model => contacts.Contains(model.EmailAddress.ToLower())).ToList());
        }

        private void ImportContacts(GoogleContact gc, List<ImportModel> contacts)
        {
            var person = GetCurrentPerson();
            var emails = new HashSet<string>(from m in contacts.Distinct() select m.EmailAddress.ToLower());

            //do we have paerson records that match these people
            var people = RavenSession.Query<Person>()
                            .Where(u => u.EmailAddress.In(emails))
                                    .ToDictionary(u => u.EmailAddress);

            using (var bulkInsert = Store.BulkInsert())
            {
                foreach (var importModel in contacts)
                {
                    //don't import yourself
                    if (importModel.EmailAddress.ToLower() == User.Identity.Name) continue;
                    
                    //if we already have a person record for this address
                    if (people.ContainsKey(importModel.EmailAddress.ToLower())) continue;

                    var newPerson = new Person
                    {
                        EmailAddress = importModel.EmailAddress.ToLower(),
                        Name = importModel.Name,
                        Id = Guid.NewGuid().ToString(),
                        Friends = new List<Friend>
                            {
                                new Friend
                                    {
                                        PersonId = person.Id,
                                        Rating = 0
                                    }
                            }
                    };
                    bulkInsert.Store(newPerson);
                    people.Add(newPerson.EmailAddress, newPerson);
                }

                person.Friends = person.Friends.Union(from i in contacts
                                                      let email = i.EmailAddress.ToLower()
                                                  where !person.Friends.Any(f => f.PersonId == people[email].Id)
                                                  select new Friend
                                                  {
                                                      PersonId = people[email].Id
                                                  }).ToList();
            }
            RavenSession.Delete(gc);
        }

        [Authorize]
        public void ImportAll()
        {
            var user = GetCurrentUser();
            var gc = RavenSession.Query<GoogleContact>().FirstOrDefault(c => c.UserId == user.Id);
            var contacts = gc.Contacts.Where(m => m.EmailAddress.ToLower() != User.Identity.Name).ToList();
            ImportContacts(gc, contacts);
        }
    }
}