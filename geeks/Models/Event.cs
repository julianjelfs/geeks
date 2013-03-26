using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace geeks.Models
{
    public class Invitation
    {
        public string UserId { get; set; }
        public bool EmailSent { get; set; }
    }

    public class Event
    {
        public Event()
        {
            Latitude = 51.509;
            Longitude = -0.115;
        }

        public Event(EventModel model)
        {
            Id = model.Id;
            Description = model.Description;
            Date = model.Date;
            Venue = model.Venue;
            CreatedBy = model.CreatedBy;
            Longitude = model.Longitude;
            Latitude = model.Latitude;
            if (model.Invitations != null)
            {
                Invitations = from i in model.Invitations
                             select new Invitation
                                 {
                                     UserId = i.UserId,
                                     EmailSent = i.EmailSent
                                 };
            }
        }

        public string Id { get; set; }
        public string Description { get; set; }
        public DateTime Date { get; set; }
        public string Venue { get; set; }
        public string CreatedBy { get; set; }
        public IEnumerable<Invitation> Invitations { get; set; }

        public double Latitude { get; set; }
        public double Longitude { get; set; }
    }

    public class EventModel
    {
        public EventModel()
        {
            Id = Guid.NewGuid().ToString();
            Invitations = new List<InvitationModel>();
            Latitude = 51.509;
            Longitude = -0.115;
            Date = DateTime.Today;
        }

        public string Id { get; set; }

        [Required]
        [DataType(DataType.MultilineText)]
        [Display(Name = "Description")]
        public string Description { get; set; }
        
        [Required]
        [DataType(DataType.DateTime)]
        [Display(Name = "Date and time")]
        public DateTime Date { get; set; }
        
        [Required]
        [DataType(DataType.MultilineText)]
        [Display(Name = "Venue")]
        public string Venue { get; set; }

        public double Latitude { get; set; }
        public double Longitude { get; set; }
        
        [DataType(DataType.Text)]
        [Display(Name = "Created By")]
        public string CreatedBy { get; set; }
        
        [DataType(DataType.Text)]
        [Display(Name = "Created By")]
        public string CreatedByUserName { get; set; }

        public bool MyEvent { get; set; }

        public List<InvitationModel> Invitations { get; set; } 
    }
}