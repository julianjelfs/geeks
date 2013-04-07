using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Web;
using System.Web.Mvc;
using Newtonsoft.Json;
using Raven.Client;
using geeks.Models;
using geeks.Services;
using Raven.Client.Linq;

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

    [ValidateJsonAntiForgeryToken]
    public class HomeController : RavenController
    {
        private readonly IEmailer _emailer;

        public HomeController(IDocumentStore store, IEmailer emailer) : base(store)
        {
            _emailer = emailer;
        }

        public virtual ActionResult Index()
        {
            return View();
        }

        public virtual ActionResult About()
        {
            return View();
        }
        
        public virtual ActionResult Calendar()
        {
            return View();
        }
        
        [HttpPost]
        [Authorize]
        public ActionResult SearchFriends(string friendSearch, bool unratedFriends)
        {
            return RedirectToAction("Friends", new { friendSearch, unratedFriends });
        }

        public virtual ActionResult Event()
        {
            return View();
        }

        protected JsonNetResult JsonNet(object data)
        {
            return new JsonNetResult
                {
                    Data = data
                };
        }

        [Authorize]
        public virtual JsonNetResult EventData(string id, string userId)
        {
            if (!string.IsNullOrEmpty(id))
            {
                var ev = RavenSession.Include<Event>(e => e.Invitations).Load<Event>(id);
                return JsonNet(EventModelFromEvent(ev, GetCurrentPerson()));
            }
            return JsonNet(new EventModel{ CreatedBy = GetCurrentUserId()});
        }


        [HttpPost]
        [Authorize]
        public virtual HttpStatusCodeResult SaveEvent(EventModel model)
        {
            if (ModelState.IsValid)
            {
                model.CreatedBy = GetCurrentUserId();
                RavenSession.Store(SendEmailToInvitees(new Event(model)));
            }
            return new HttpStatusCodeResult(HttpStatusCode.OK);
        }

        private EventModel EventModelFromEvent(Event ev, Person currentPerson = null)
        {
            return new EventModel
                {
                    Id = ev.Id,
                    CreatedByUserName = RavenSession.Load<User>(ev.CreatedBy).Username,
                    CreatedBy = ev.CreatedBy,
                    Date = ev.Date,
                    Description = ev.Description,
                    Latitude = ev.Latitude,
                    Longitude = ev.Longitude,
                    Venue = ev.Venue,
                    Invitations = (from i in ev.Invitations
                               let person = RavenSession.Load<Person>(i.PersonId)
                               let friend = GetFriendFromPerson(currentPerson, i.PersonId)
                               select new InvitationModel
                                   {
                                       Email = person.EmailAddress,
                                       PersonId = person.Id,
                                       Rating = friend == null ? 0 : friend.Rating,
                                       EmailSent = i.EmailSent
                                   }).ToList()
                };
        }

        private Friend GetFriendFromPerson(Person person, string friendPersonId)
        {
            if (person == null)
                return null;
            return person.Friends.SingleOrDefault(f => f.PersonId == friendPersonId);
        }

        private Event SendEmailToInvitees(Event ev)
        {
            var invitees = ev.Invitations.ToArray();
            var users = RavenSession.Query<User>()
                                    .Where(u => u.Username.In(from i in invitees
                                                              where !i.EmailSent
                                                              select i.PersonId));

            foreach (var invitee in invitees.Where(i => !i.EmailSent))
            {
                _emailer.Invite(GetCurrentPerson(), RavenSession.Load<Person>(invitee.PersonId), ev);
                invitee.EmailSent = true;
            }
            ev.Invitations = invitees;
            return ev;
        }

        [Authorize]
        public virtual JsonNetResult EventsData(int pageIndex = 0, int pageSize = 10, string search = null)
        {
            var evs = RavenSession.Query<Event>()
                                  .Include<Event>(e => e.Invitations)
                                  .Include<Event>(e => e.CreatedBy)
                                  .Where(e => e.CreatedBy == GetCurrentUserId())
                                  .ToList();
            var models = (from e in evs select EventModelFromEvent(e)).ToList();
            var total = (int)Math.Ceiling((double)models.Count() / pageSize);
            return JsonNet(new
            {
                Events = models.Skip(pageIndex * pageSize).Take(pageSize),
                NumberOfPages = total,
                SearchTerm = search,
                PageIndex = pageIndex
            });
        }

        [Authorize]
        public virtual ActionResult Events()
        {
            return View();
        }

        [HttpPost]
        [Authorize]
        public void DeleteEvent(int id)
        {
            var model = RavenSession.Load<EventModel>(id);
            if (model != null)
            {
                RavenSession.Delete(model);
            }
        }

        [HttpPost]
        [Authorize]
        public virtual void DeleteAllFriends(string id)
        {
            GetCurrentPerson().Friends.Clear();
        }

        [HttpPost]
        [Authorize]
        public virtual void AddFriend(string name, string email)
        {
            var person = CreatePersonDocumentIfNecessary(name, email);

            var me = RavenSession.Query<Person>()
                        .Include<Person>(p => p.Friends.Select(f => f.PersonId))
                        .SingleOrDefault(p => p.EmailAddress == User.Identity.Name);

            var friend = me.Friends.SingleOrDefault(f => RavenSession.Load<Person>(f.PersonId).EmailAddress == email);
            if (friend == null)
                me.Friends.Add(new Friend {PersonId = person.Id});

            RavenSession.SaveChanges();
        }
        
        [HttpPost]
        [Authorize]
        public virtual void RateFriend(string id, string rating)
        {
            var me = RavenSession.Query<Person>()
                        .FirstOrDefault(p => p.EmailAddress == User.Identity.Name);

            var friend = me.Friends.SingleOrDefault(f => f.PersonId == id);
            if (friend != null)
            {
                friend.Rating = Convert.ToInt32(rating);
            }
        }

        private Person CreatePersonDocumentIfNecessary(string name, string email)
        {
            var person = RavenSession.Query<Person>()
                                   .FirstOrDefault(p => p.EmailAddress == email);

            if (person == null)
            {
                person = new Person
                    {
                        Id = Guid.NewGuid().ToString(),
                        EmailAddress = email,
                        Name = name
                    };
                RavenSession.Store(person);
            }
            return person;
        }

        [HttpPost]
        [Authorize]
        [ValidateJsonAntiForgeryToken]
        public virtual void DeleteFriend(string id)
        {
            GetCurrentPerson().Friends.RemoveAll(f => f.PersonId == id);
        }

        private IEnumerable<PersonFriend> FriendsInternal(int pageIndex, int pageSize, out int totalPages, string friendSearch, bool unratedFriends)
        {
            return PersonsFromFriends(GetCurrentPersonId(), pageIndex, pageSize, out totalPages, friendSearch, unratedFriends);
        }

        [Authorize]
        public virtual JsonNetResult FriendsData(int pageIndex = 0, int pageSize = 10, string friendSearch = null, bool unratedFriends = false)
        {
            int total = 0;
            var friends = FriendsInternal(pageIndex, pageSize, out total, friendSearch, unratedFriends);
            return JsonNet(new
            {
                Friends = friends,
                NumberOfPages = total,
                SearchTerm = friendSearch,
                Unrated = unratedFriends,
                PageIndex = pageIndex
            });
        }

        [Authorize]
        public ViewResult Friends()
        {
            return View();
        }

        [Authorize]
        public JsonNetResult LookupFriends(string query)
        {
            var totalPages = 0;
            var matches = PersonsFromFriends(GetCurrentPersonId(), 0, 100, out totalPages, query, false);
            var dict = new Dictionary<string, object>();
            foreach (var match in matches.Where(match => !dict.ContainsKey(match.Email)))
                dict.Add(match.Email, new {
                    match.PersonId, 
                    match.Rating
                });
            return JsonNet(dict);
        }
    }
}
