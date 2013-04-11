using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace geeks.Models
{
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

        public bool ReadOnly { get; set; }
        public double Score { get; set; }

        public List<InvitationModel> Invitations { get; set; } 
    }
}