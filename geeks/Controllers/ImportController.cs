using System;
using System.Web.Mvc;
using DotNetOpenAuth.Messaging;
using DotNetOpenAuth.OAuth2;
using Google.Contacts;
using Google.GData.Client;

namespace geeks.Controllers
{
    public class AuthHelper
    {
        public static WebServerClient CreateClient()
        {
            AuthorizationServerDescription desc = GetAuthServerDescription();
            var client = new WebServerClient(desc, clientIdentifier: "475779814525.apps.googleusercontent.com");
            client.ClientCredentialApplicator = ClientCredentialApplicator.PostParameter("plCPGSN2a218q7gHYmy0-BW1");
            return client;
        }

        public static AuthorizationServerDescription GetAuthServerDescription()
        {
            var authServerDescription = new AuthorizationServerDescription();
            authServerDescription.AuthorizationEndpoint = new Uri(@"https://accounts.google.com/o/oauth2/auth");
            authServerDescription.TokenEndpoint = new Uri(@"https://accounts.google.com/o/oauth2/token");
            authServerDescription.ProtocolVersion = ProtocolVersion.V20;
            return authServerDescription;
        }
    }

    public class ImportController : RavenController
    {
        private static readonly WebServerClient client = AuthHelper.CreateClient();

        [AllowAnonymous]
        public ActionResult GoogleAuthCallback(string returnUrl)
        {
            return Redirect(returnUrl);
        }

        public ActionResult OAuth()
        {
            if (string.IsNullOrEmpty(Request.QueryString["code"]))
            {
                return InitAuth();
            }
            return OAuthCallback();
        }

        private ActionResult InitAuth()
        {
            var state = new AuthorizationState();
            string uri = Request.Url.AbsoluteUri;
            uri = RemoveQueryStringFromUri(uri);
            state.Callback = new Uri(uri);

//            state.Scope.Add("https://www.googleapis.com/auth/userinfo.profile");
//            state.Scope.Add("https://www.googleapis.com/auth/userinfo.email");
//            state.Scope.Add("https://www.googleapis.com/auth/calendar");
            state.Scope.Add("https://www.google.com/m8/feeds");
            OutgoingWebResponse r = client.PrepareRequestUserAuthorization(state);
            return r.AsActionResult();
        }

        private static string RemoveQueryStringFromUri(string uri)
        {
            int index = uri.IndexOf('?');
            if (index > -1)
            {
                uri = uri.Substring(0, index);
            }
            return uri;
        }

        private ActionResult OAuthCallback()
        {
            IAuthorizationState auth = client.ProcessUserAuthorization(Request);
            Session["auth"] = auth;


            var settings = new RequestSettings("<var>Geeks Dilemma</var>", auth.AccessToken);
            // Add authorization token.
            // ...

            var cr = new ContactsRequest(settings);
            Feed<Contact> contacts = cr.GetContacts();
            return null;
        }


        [Authorize]
        public ActionResult ImportFacebook()
        {
            ViewBag.ImportFrom = "Facebook";
            return View("Import");
        }

        [Authorize]
        public ActionResult ImportTwitter()
        {
            ViewBag.ImportFrom = "Twitter";
            return View("Import");
        }
    }
}