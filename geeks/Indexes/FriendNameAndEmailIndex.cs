using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Raven.Client.Indexes;
using geeks.Models;

namespace geeks.Indexes
{
    public class FriendNameAndEmailIndex : AbstractIndexCreationTask<User>
    {
        public FriendNameAndEmailIndex()
        {
            Map = users => from u in users
                           select new
                               {
                                   u.Username,
                                   Friends = from f in u.Friends
                                             select LoadDocument<User>(f.UserId)
                               };
        }
    }
}