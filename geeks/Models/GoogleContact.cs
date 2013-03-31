using System.Collections.Generic;

namespace geeks.Models
{
    public class GoogleContact
    {
        public string Id { get; set; }
        public string UserId { get; set; }
        public List<ImportModel> Contacts { get; set; } 
    }
}