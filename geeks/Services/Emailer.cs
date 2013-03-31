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
        bool Invite(Person organiser, Person user, Event ev);
    }

    public class FakeEmailer : IEmailer
    {
        public bool Invite(Person organiser, Person user, Event ev)
        {
            return true;
        }
    }

    public class Emailer : IEmailer
    {
        public bool Invite(Person organiser, Person user, Event ev)
        {
            var message = new MailMessage();
            message.To.Add(user.EmailAddress);
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
                "http://localhost/geeks/event/" + ev.Id  + "/" + user.Id;
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

        private static string DisplayName(Person organiser)
        {
            return string.IsNullOrEmpty(organiser.Name)
                ? organiser.EmailAddress : organiser.Name;
        }
    }
}