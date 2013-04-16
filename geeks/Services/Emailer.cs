using System;
using System.Collections.Generic;
using System.Net.Mail;
using geeks.Models;

namespace geeks.Services
{
    public interface IEmailer
    {
        bool Invite(Person organiser, Person user, Event ev);
        bool UpdateInvitationResponse(Event ev, Person organiser, Person responder, InvitationResponse response);
        void EventIsOff(Event ev, Person organiser, List<Person> invitees);
        void EventIsOn(Event ev, Person organiser, List<Person> invitees);
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
                "http://localhost/geeks/home/event/" + ev.Id  + "/" + user.Id;
            var smtp = new SmtpClient();
            smtp.Send(message);

            return true;
        }

        public void EventIsOn(Event ev, Person organiser, List<Person> invitees)
        {
            var message = new MailMessage();
            var score = ev.PercentageScore();
            message.To.Add(organiser.EmailAddress);
            foreach (var invitee in invitees)
            {
                message.CC.Add(invitee.EmailAddress);
            }
            message.From = new MailAddress("robot@geeksdilemma.com");
            message.Subject = string.Format("Your Geek's Dilemma event has a green light with a score of {0}%", score);
            message.Body = @"The following event is looking good with a current score of "+ score  +@"%." +
                Environment.NewLine +
                Environment.NewLine +
                ev.Description +
                Environment.NewLine +
                Environment.NewLine +
                "To look at the details of this event, rate friends or change your attendance, click the following link:" +
                Environment.NewLine +
                Environment.NewLine +
                "http://localhost/geeks/home/event/" + ev.Id;
            var smtp = new SmtpClient();
            smtp.Send(message);
        }
        
        public void EventIsOff(Event ev, Person organiser, List<Person> invitees)
        {
            var message = new MailMessage();
            var score = ev.PercentageScore();
            message.To.Add(organiser.EmailAddress);
            foreach (var invitee in invitees)
            {
                message.CC.Add(invitee.EmailAddress);
            }
            message.From = new MailAddress("robot@geeksdilemma.com");
            message.Subject = string.Format("Your Geek's Dilemma event has a red light with a score of {0}%", score);
            message.Body = @"The following event is not looking too good with a current score of "+ score  +@"%." +
                Environment.NewLine +
                Environment.NewLine +
                ev.Description +
                Environment.NewLine +
                Environment.NewLine +
                "To look at the details of this event, rate friends or change your attendance, click the following link:" +
                Environment.NewLine +
                Environment.NewLine +
                "http://localhost/geeks/home/event/" + ev.Id;
            var smtp = new SmtpClient();
            smtp.Send(message);
        }
        
        public bool UpdateInvitationResponse(Event ev, Person organiser, Person responder, InvitationResponse response)
        {
            var message = new MailMessage();
            message.To.Add(organiser.EmailAddress);
            message.From = new MailAddress("robot@geeksdilemma.com");
            message.Subject = string.Format("{0} has responded to your event invitation", DisplayName(responder));
            message.Body = @"Your friend " + DisplayName(responder) + @" has " + (response == InvitationResponse.Yes ? "accepted":"declined") + @" your invitation to the following event:" +
                Environment.NewLine +
                Environment.NewLine +
                ev.Description +
                Environment.NewLine +
                Environment.NewLine +
                "To see how this may have affected the score for this event click the following link:" +
                Environment.NewLine +
                Environment.NewLine +
                "http://localhost/geeks/home/event/" + ev.Id;
            var smtp = new SmtpClient();
            smtp.Send(message);
            return true;
        }

        private static string DisplayName(Person organiser)
        {
            return string.IsNullOrEmpty(organiser.Name)
                ? organiser.EmailAddress : organiser.Name;
        }
    }
}