using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace geeks.Models
{
    public class Event
    {
        public Event()
        {
            
        }

        public Event(EventModel model)
        {
            Id = model.Id;
            Title = model.Title;
            Description = model.Description;
            Date = model.Date;
            Venue = model.Venue;
            CreatedBy = model.CreatedBy;
            if (model.Invitees != null)
            {
                InviteeIds = from i in model.Invitees
                             select i.UserId;
            }
        }

        public string Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public DateTime Date { get; set; }
        public string Venue { get; set; }
        public string CreatedBy { get; set; }
        public IEnumerable<string> InviteeIds { get; set; }
    }

    public class EventModel
    {
        public EventModel()
        {
            Id = Guid.NewGuid().ToString();
            Invitees = new List<UserFriend>();
        }

        public string Id { get; set; }

        [Required]
        [DataType(DataType.Text)]
        [Display(Name = "Title")]
        public string Title { get; set; }
        
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
        
        [DataType(DataType.Text)]
        [Display(Name = "Created By")]
        public string CreatedBy { get; set; }
        
        [DataType(DataType.Text)]
        [Display(Name = "Created By")]
        public string CreatedByUserName { get; set; }

        public List<UserFriend> Invitees { get; set; } 
    }
}