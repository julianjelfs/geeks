using System;
using System.Collections.Generic;
using System.Linq;
using Ninject;
using Ninject.Activation;
using Ninject.Modules;
using Ninject.Web.Common;
using Raven.Abstractions.Indexing;
using Raven.Client;
using Raven.Client.Document;
using Raven.Client.Embedded;
using Raven.Client.Indexes;
using Raven.Database.Server;
using geeks.Indexes;
using geeks.Models;

namespace geeks.DependencyResolution
{
    public class Event_ByDescription : AbstractIndexCreationTask<Event>
    {
        public Event_ByDescription()
        {
            Map = events => from ev in events
                            select new
                                {
                                    ev.Description,
                                    ev.Venue,
                                    ev.CreatedBy,
                                    Invitations_PersonId = from i in ev.Invitations select i.PersonId
                                };

            Index(x => x.Description, FieldIndexing.Analyzed);
            Index(x => x.Venue, FieldIndexing.Analyzed);
        }
    }

    public class RavenModule : NinjectModule
    {
        public override void Load()
        {
            Bind<IDocumentStore>()
                .ToMethod(InitDocStore)
                .InSingletonScope();

            Bind<IDocumentSession>()
                .ToMethod(c => c.Kernel.Get<IDocumentStore>().OpenSession())
                .InRequestScope();
        }

        private IDocumentStore InitDocStore(IContext context)
        {
            var store = new DocumentStore
                {
                    ConnectionStringName = "LocalRavenDB"
                };
            store.Initialize();
            IndexCreation.CreateIndexes(typeof(Event_ByDescription).Assembly, store);
            return store;
        }
    }
}