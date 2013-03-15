using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Web;
using geeks.Models;

namespace geeks.Services
{
    public interface IEmailer
    {
        bool Invite(User organiser, IEnumerable<User> users, Event ev);
    }

    public class FakeEmailer : IEmailer
    {
        public bool Invite(User organiser, IEnumerable<User> users, Event ev)
        {
            return true;
        }
    }

    public class Emailer : IEmailer
    {
        public bool Invite(User organiser, IEnumerable<User> users, Event ev)
        {
            var message = new MailMessage();
            foreach (var user in users)
            {
                message.Bcc.Add(user.Username);
            }
            message.From = new MailAddress("robot@geeksdilemma.com");
            message.Subject = string.Format("You have been invited to a Geeks Dilemma event by {0}", DisplayName(organiser));
            message.Body = @"Your friend " + DisplayName(organiser) + @" is organising an event and you're invited. " +
                Environment.NewLine + 
                Environment.NewLine + 
                ev.Description +
                Environment.NewLine +
                Environment.NewLine + 
                "To see the details including who else is invitee and to say whether or not you can go, click here:"+
                Environment.NewLine +
                Environment.NewLine +
                "http://localhost/geeks/event/" + ev.Id;
            var smtp = new SmtpClient();
            //try
            //{
                smtp.Send(message);
            //}
            //catch (SmtpFailedRecipientException exception)
            //{
            //    throw new ApplicationException(string.Format("The email address {0} does not seem to be reachable. Please make sure that the email address associated with your friend is correct.", user.Username), exception);
            //}

            return true;
        }

        private static string DisplayName(User organiser)
        {
            return string.IsNullOrEmpty(organiser.Name)
                ? organiser.Username : organiser.Name;
        }
    }
}