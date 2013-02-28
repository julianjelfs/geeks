using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DotNetOpenAuth.AspNet.Clients;
using FlexProviders.Membership;
using Microsoft.Web.WebPages.OAuth;
using geeks.Models;

namespace geeks
{
    public static class AuthConfig
    {
        public static void RegisterAuth()
        {
            // To let users of this site log in using their accounts from other sites such as Microsoft, Facebook, and Twitter,
            // you must update this site. For more information visit http://go.microsoft.com/fwlink/?LinkID=252166

            //OAuthWebSecurity.RegisterMicrosoftClient(
            //    clientId: "",
            //    clientSecret: "");

            //OAuthWebSecurity.RegisterTwitterClient(
            //    consumerKey: "",
            //    consumerSecret: "");

//            OAuthWebSecurity.RegisterFacebookClient(
//                appId: "124059391108141",
//                appSecret: "5d9eaa551aeea2f3e9e5c9fe728bd30b");
//
//            OAuthWebSecurity.RegisterGoogleClient();

            FlexMembershipProvider.RegisterClient(
                new GoogleOpenIdClient(),
                "Google", new Dictionary<string, object>());
        }
    }
}
