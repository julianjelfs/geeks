using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace geeks.Models
{
    public class EventModel
    {
        public int Id { get; set; }

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
    }
}