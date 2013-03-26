using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using FlexProviders.Membership;

namespace geeks.Models
{
    public class Friend
    {
        public int Rating { get; set; }
        public string UserId { get; set; }
    }

    public class UserFriend
    {
        public string UserId { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public int Rating { get; set; }
    }
    
    public class InvitationModel
    {
        public string UserId { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public int Rating { get; set; }
        public bool EmailSent { get; set; }
        public bool CanRate { get; set; }
    }

    public class User : IFlexMembershipUser
    {
        public User()
        {
            OAuthAccounts = new Collection<FlexOAuthAccount>();
            Friends = new List<Friend>();
        }

        public string Id { get; set; }
        public string Username { get; set; }    
        public string Name { get; set; }
        public string Password { get; set; }
        public string Salt { get; set; }
        public string PasswordResetToken { get; set; }
        public DateTime PasswordResetTokenExpiration { get; set; }
        public virtual ICollection<FlexOAuthAccount> OAuthAccounts { get; set; }
        public bool Registered { get; set; }
        public List<Friend> Friends { get; set; } 

        public override string ToString()
        {
            return Username;
        }
    }
}