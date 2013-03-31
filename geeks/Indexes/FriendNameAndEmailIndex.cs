using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Raven.Client.Indexes;
using geeks.Models;

namespace geeks.Indexes
{
    public class UserAndFriend
    {
        public string Username { get; set; }
        public string FriendUsername { get; set; }
        public string FriendName { get; set; }    
    }
}