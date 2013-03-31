using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using FlexProviders.Membership;

namespace geeks.Models
{
    public class Friend
    {
        public int Rating { get; set; }
        public string PersonId { get; set; }
    }

    /// <summary>
    /// we need to be able to represent a person in the system before they 
    /// have a user account, so we need a separate class
    /// </summary>
    public class Person
    {
        public Person()
        {
            Friends = new List<Friend>();
        }
        public string Id { get; set; }
        public string UserId { get; set; }
        public string Name { get; set; }
        public string EmailAddress { get; set; }
        public List<Friend> Friends { get; set; } 
    }

    public class User : IFlexMembershipUser
    {
        public User()
        {
            OAuthAccounts = new Collection<FlexOAuthAccount>();
        }

        public string Id { get; set; }
        public string Username { get; set; }    
        public string Password { get; set; }
        public string Salt { get; set; }
        public string PasswordResetToken { get; set; }
        public DateTime PasswordResetTokenExpiration { get; set; }
        public virtual ICollection<FlexOAuthAccount> OAuthAccounts { get; set; }

        public override string ToString()
        {
            return Username;
        }
    }
}