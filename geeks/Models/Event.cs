using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace geeks.Models
{
    public class Event
    {
        public Event()
        {
            Latitude = 51.509;
            Longitude = -0.115;
        }

        public Event Merge(EventModel model)
        {
            Id = model.Id;
            Description = model.Description;
            Date = model.Date;
            Time = model.Time;
            Venue = model.Venue;
            CreatedBy = model.CreatedBy;
            Longitude = model.Longitude;
            Latitude = model.Latitude;
            if (model.Invitations != null)
            {
                Invitations = from i in model.Invitations
                              select new Invitation
                              {
                                  PersonId = i.PersonId,
                                  EmailSent = i.EmailSent,
                                  Response = i.Response
                              };
            }
            return this;
        }

        public double PercentageScore()
        {
            var score = 0D;
            if (TheoreticalMaximumScore > 0)
                score = (Score / TheoreticalMaximumScore) * 100;
            return score;
        }

        public Event(EventModel model)
        {
            Merge(model);
        }

        public string Id { get; set; }
        public string Description { get; set; }
        public DateTime Date { get; set; }
        public string Time { get; set; }
        public string Venue { get; set; }
        public string CreatedBy { get; set; }
        public IEnumerable<Invitation> Invitations { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public double TheoreticalMaximumScore { get; set; }
        public double Score { get; set; }
        public double EveryoneComingScore { get; set; }
    }
}