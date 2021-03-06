﻿using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Web.Mvc;
using DotNetOpenAuth.AspNet;
using FlexProviders.Membership;
using Microsoft.Web.WebPages.OAuth;
using Raven.Client;
using geeks.Models;
using System.Linq;

namespace geeks.Controllers
{
    [Authorize]
    public class AccountController : RavenController
    {
        private readonly IFlexMembershipProvider _membershipProvider;
        private readonly IFlexOAuthProvider _oAuthProvider;
        private readonly ISecurityEncoder _encoder = new DefaultSecurityEncoder();

        public AccountController(IFlexMembershipProvider membership, 
            IFlexOAuthProvider oauth, 
            IDocumentStore store)
            : base(store)
        {
            _membershipProvider = membership;
            _oAuthProvider = oauth;
        }
        //
        // GET: /Account/Login

        [AllowAnonymous]
        public virtual ActionResult Login(string returnUrl)
        {
            var u = ExtractUserFromReturnUrl(returnUrl);
            LoginModel user = null;
            if (u != null)
            {
                user = new LoginModel
                {
                    UserName = u.Username
                };
            }

            ViewBag.ReturnUrl = returnUrl;
            return View(user);
        }

        /// <summary>
        /// when we get here we may have come in by clicking on an event link. If
        /// so the return url should be /geeks/event/{eventId}/{userId}
        /// </summary>
        private User ExtractUserFromReturnUrl(string returnUrl)
        {
            if (string.IsNullOrEmpty(returnUrl))
                return null;

            var pattern = @"^/geeks/event/.+/(.+)$";
            var match = Regex.Match(returnUrl, pattern, RegexOptions.IgnoreCase);
            if (match.Success)
            {
                return RavenSession.Load<User>(match.Groups[1].Value);
            }
            return null;
        }

        //
        // POST: /Account/Login

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public virtual ActionResult Login(LoginModel model, string returnUrl)
        {
            Session["UserId"] = null;
            Session["PersonId"] = null;
            if (ModelState.IsValid && _membershipProvider.Login(model.UserName, model.Password, model.RememberMe))
            {
                return RedirectToLocal(returnUrl);
            }

            // If we got this far, something failed, redisplay form
            ModelState.AddModelError("", "The user name or password provided is incorrect.");
            return View(model);
        }

        //
        // POST: /Account/LogOff

        [HttpPost]
        public virtual ActionResult LogOff()
        {
            Session["UserId"] = null;
            Session["PersonId"] = null;
            _membershipProvider.Logout();
            return RedirectToAction("Index", "Home");
        }

        //
        // GET: /Account/Register

        [AllowAnonymous]
        public virtual ActionResult Register(string returnUrl)
        {
            ViewBag.ReturnUrl = returnUrl;
            RegisterModel user;
            var u = ExtractUserFromReturnUrl(returnUrl);
            if (u != null)
            {
                user = new RegisterModel
                    {
                        EmailAddress = u.Username
                    };
                return View(user);
            }
            return View();
        }

        //
        // POST: /Account/Register

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public virtual ActionResult Register(RegisterModel model, string returnUrl)
        {
            if (ModelState.IsValid)
            {
                // Attempt to register the user
                try
                {
                    var user = new User
                        {
                            Id = Guid.NewGuid().ToString(),
                            Username = model.EmailAddress.ToLower(),
                            Password = model.Password
                        };
                    CreateOrUpdatePersonRecord(model.Name, user);
                    _membershipProvider.CreateAccount(user);
                    _membershipProvider.Login(user.Username, model.Password);
                    Session["UserId"] = null;
                    Session["PersonId"] = null;
                    return RedirectToLocal(returnUrl);
                }
                catch (FlexMembershipException e)
                {
                    ModelState.AddModelError("", ErrorCodeToString(e.StatusCode));
                }
            }

            // If we got this far, something failed, redisplay form
            return View(model);
        }

        private void CreateOrUpdatePersonRecord(string name, User user)
        {
            var person =
                RavenSession.Query<Person>()
                            .FirstOrDefault(
                                p => p.EmailAddress.Equals(user.Username, StringComparison.InvariantCultureIgnoreCase))
                ?? new Person
                    {
                        Id = Guid.NewGuid().ToString(),
                        EmailAddress = user.Username.ToLower(),
                        Name = name
                    };

            person.UserId = user.Id;
            RavenSession.Store(person);
        }

        //
        // POST: /Account/Disassociate

        [HttpPost]
        [ValidateAntiForgeryToken]
        public virtual ActionResult Disassociate(string provider, string providerUserId)
        {
            _oAuthProvider.DisassociateOAuthAccount(provider, providerUserId);
            return RedirectToAction("Manage", new { Message = "Complete" });
        }

        //
        // GET: /Account/Manage

        public virtual ActionResult Manage(ManageMessageId? message)
        {
            ViewBag.StatusMessage =
                message == ManageMessageId.ChangePasswordSuccess ? "Your password has been changed."
                : message == ManageMessageId.SetPasswordSuccess ? "Your password has been set."
                : message == ManageMessageId.RemoveLoginSuccess ? "The external login was removed."
                : "";
            ViewBag.HasLocalPassword = _membershipProvider.HasLocalAccount(User.Identity.Name);
            ViewBag.ReturnUrl = Url.Action("Manage");
            return View();
        }

        //
        // POST: /Account/Manage

        [HttpPost]
        [ValidateAntiForgeryToken]
        public virtual ActionResult Manage(LocalPasswordModel model)
        {
            bool hasLocalAccount = _membershipProvider.HasLocalAccount(User.Identity.Name);
            ViewBag.HasLocalPassword = hasLocalAccount;
            ViewBag.ReturnUrl = Url.Action("Manage");
            if (hasLocalAccount)
            {
                if (ModelState.IsValid)
                {
                    // ChangePassword will throw an exception rather than return false in certain failure scenarios.
                    bool changePasswordSucceeded;
                    try
                    {
                        changePasswordSucceeded = _membershipProvider.ChangePassword(User.Identity.Name, model.OldPassword, model.NewPassword);
                    }
                    catch (Exception)
                    {
                        changePasswordSucceeded = false;
                    }

                    if (changePasswordSucceeded)
                    {
                        return RedirectToAction("Manage", new { Message = ManageMessageId.ChangePasswordSuccess });
                    }
                    else
                    {
                        ModelState.AddModelError("", "The current password is incorrect or the new password is invalid.");
                    }
                }
            }
            else
            {
                // User does not have a local password so remove any validation errors caused by a missing
                // OldPassword field
                ModelState state = ModelState["OldPassword"];
                if (state != null)
                {
                    state.Errors.Clear();
                }

                if (ModelState.IsValid)
                {
                    try
                    {
                        _membershipProvider.SetLocalPassword(User.Identity.Name, model.NewPassword);
                        return RedirectToAction("Manage", new { Message = ManageMessageId.SetPasswordSuccess });
                    }
                    catch (FlexMembershipException e)
                    {
                        ModelState.AddModelError("", e);
                    }
                }
            }

            // If we got this far, something failed, redisplay form
            return View(model);
        }

        //
        // POST: /Account/ExternalLogin

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public virtual ActionResult ExternalLogin(string provider, string returnUrl)
        {
            return new ExternalLoginResult(_oAuthProvider, provider, Url.Action("ExternalLoginCallback", new { ReturnUrl = returnUrl }));
        }

        //
        // GET: /Account/ExternalLoginCallback

        [AllowAnonymous]
        public virtual ActionResult ExternalLoginCallback(string returnUrl)
        {
            Session["UserId"] = null;
            Session["PersonId"] = null;

            AuthenticationResult result = _oAuthProvider.VerifyOAuthAuthentication(Url.Action("ExternalLoginCallback", new { ReturnUrl = returnUrl }));
            if (!result.IsSuccessful)
            {
                return RedirectToAction("ExternalLoginFailure");
            }

            if (_oAuthProvider.OAuthLogin(result.Provider, result.ProviderUserId, persistCookie: false))
            {
                return RedirectToLocal(returnUrl);
            }

            if (User.Identity.IsAuthenticated)
            {
                // If the current user is logged in add the new account
                var user = new User()
                    {
                        Username = User.Identity.Name, Id = Guid.NewGuid().ToString()
                    };
                _oAuthProvider.CreateOAuthAccount(result.Provider, result.ProviderUserId, user);
                CreateOrUpdatePersonRecord(user.Username, user);
                return RedirectToLocal(returnUrl);
            }
            else
            {
                // User is new, ask for their desired membership name
                string loginData = _encoder.SerializeOAuthProviderUserId(result.Provider, result.ProviderUserId);
                ViewBag.ProviderDisplayName = _oAuthProvider.GetOAuthClientData(result.Provider).DisplayName;
                ViewBag.ReturnUrl = returnUrl;
                return View("ExternalLoginConfirmation", new RegisterExternalLoginModel { UserName = result.UserName, ExternalLoginData = loginData });
            }
        }

        //
        // POST: /Account/ExternalLoginConfirmation

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public virtual ActionResult ExternalLoginConfirmation(RegisterExternalLoginModel model, string returnUrl)
        {
            string provider = null;
            string providerUserId = null;

            Session["UserId"] = null;
            Session["PersonId"] = null;

            if (User.Identity.IsAuthenticated || !_encoder.TryDeserializeOAuthProviderUserId(model.ExternalLoginData, out provider, out providerUserId))
            {
                return RedirectToAction("Manage");
            }

            if (ModelState.IsValid)
            {
                if (!_membershipProvider.HasLocalAccount(model.UserName))
                {
                    var user = new User
                        {
                            Username = model.UserName, Id = Guid.NewGuid().ToString()
                        };
                    _oAuthProvider.CreateOAuthAccount(provider, providerUserId, user);
                    _oAuthProvider.OAuthLogin(provider, providerUserId, persistCookie: false);
                    CreateOrUpdatePersonRecord(user.Username, user);
                    return RedirectToLocal(returnUrl);
                }
                else
                {
                    ModelState.AddModelError("UserName", "User name already exists. Please enter a different user name.");
                }
            }

            ViewBag.ProviderDisplayName = _oAuthProvider.GetOAuthClientData(provider).DisplayName;
            ViewBag.ReturnUrl = returnUrl;
            return View(model);
        }

        //
        // GET: /Account/ExternalLoginFailure

        [AllowAnonymous]
        public virtual ActionResult ExternalLoginFailure()
        {
            return View();
        }

        [AllowAnonymous]
        [ChildActionOnly]
        public virtual ActionResult ExternalLoginsList(string returnUrl)
        {
            ViewBag.ReturnUrl = returnUrl;
            return PartialView("_ExternalLoginsListPartial", _oAuthProvider.RegisteredClientData);
        }

        [ChildActionOnly]
        public virtual ActionResult RemoveExternalLogins()
        {
            var accounts = _oAuthProvider.GetOAuthAccountsFromUserName(User.Identity.Name);
            List<ExternalLogin> externalLogins = new List<ExternalLogin>();
            foreach (OAuthAccount account in accounts)
            {
                AuthenticationClientData clientData = _oAuthProvider.GetOAuthClientData(account.Provider);

                externalLogins.Add(new ExternalLogin
                {
                    Provider = account.Provider,
                    ProviderDisplayName = clientData.DisplayName,
                    ProviderUserId = account.ProviderUserId,
                });
            }

            ViewBag.ShowRemoveButton = externalLogins.Count > 1 || _membershipProvider.HasLocalAccount(User.Identity.Name);
            return PartialView("_RemoveExternalLoginsPartial", externalLogins);
        }

        #region Helpers
        private ActionResult RedirectToLocal(string returnUrl)
        {
            if (Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            else
            {
                return RedirectToAction("Index", "Home");
            }
        }

        internal class ExternalLoginResult : ActionResult
        {
            private IFlexOAuthProvider _oAuthProvider;

            public ExternalLoginResult(IFlexOAuthProvider oAuthProvider, string provider, string returnUrl)
            {
                _oAuthProvider = oAuthProvider;
                Provider = provider;
                ReturnUrl = returnUrl;
            }

            public string Provider { get; private set; }
            public string ReturnUrl { get; private set; }

            public override void ExecuteResult(ControllerContext context)
            {
                _oAuthProvider.RequestOAuthAuthentication(Provider, ReturnUrl);
            }
        }

        private static string ErrorCodeToString(FlexMembershipStatus createStatus)
        {
            // See http://go.microsoft.com/fwlink/?LinkID=177550 for
            // a full list of status codes.
            switch (createStatus)
            {
                case FlexMembershipStatus.DuplicateUserName:
                    return "User name already exists. Please enter a different user name.";

                case FlexMembershipStatus.DuplicateEmail:
                    return "A user name for that e-mail address already exists. Please enter a different e-mail address.";

                case FlexMembershipStatus.InvalidPassword:
                    return "The password provided is invalid. Please enter a valid password value.";

                case FlexMembershipStatus.InvalidEmail:
                    return "The e-mail address provided is invalid. Please check the value and try again.";

                case FlexMembershipStatus.InvalidAnswer:
                    return "The password retrieval answer provided is invalid. Please check the value and try again.";

                case FlexMembershipStatus.InvalidQuestion:
                    return "The password retrieval question provided is invalid. Please check the value and try again.";

                case FlexMembershipStatus.InvalidUserName:
                    return "The user name provided is invalid. Please check the value and try again.";

                case FlexMembershipStatus.ProviderError:
                    return "The authentication provider returned an error. Please verify your entry and try again. If the problem persists, please contact your system administrator.";

                case FlexMembershipStatus.UserRejected:
                    return "The user creation request has been canceled. Please verify your entry and try again. If the problem persists, please contact your system administrator.";

                default:
                    return "An unknown error occurred. Please verify your entry and try again. If the problem persists, please contact your system administrator.";
            }
        }

        public enum ManageMessageId
        {
            ChangePasswordSuccess,
            SetPasswordSuccess,
            RemoveLoginSuccess,
        }
        #endregion
    }
}
