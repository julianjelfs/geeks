﻿using System.Collections.Generic;
using System.Linq;
using Raven.Client;
using geeks.Models;

namespace geeks.Queries
{
    public class EventsFeaturingPerson : Query<IEnumerable<Event>>
    {
        public string PersonId { get; set; }

        public override IEnumerable<Event> Execute()
        {
            return Session.Query<Event>()
                          .Include<Event>(e => e.Invitations.Select(i => i.PersonId))
                          .Where(e => e.Invitations.Any(i => i.PersonId == PersonId));
        }
    }
}