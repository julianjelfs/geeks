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

    public class FriendNameAndEmailIndex : AbstractIndexCreationTask<User>
    {
        public FriendNameAndEmailIndex()
        {
            Map = users => from u in users
                           from f in u.Friends
                           select new UserAndFriend
                               {
                                   Username = u.Username,
                                   FriendUsername = LoadDocument<User>(f.UserId).Username,
                                   FriendName = LoadDocument<User>(f.UserId).Name
                               };
        }
    }
}