using Ninject;
using Ninject.Activation;
using Ninject.Modules;
using Ninject.Web.Common;
using Raven.Client;
using Raven.Client.Document;
using Raven.Client.Embedded;
using Raven.Client.Indexes;
using Raven.Database.Server;
using geeks.Indexes;

namespace geeks.DependencyResolution
{
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
                }.Initialize();
            IndexCreation.CreateIndexes(typeof(FriendNameAndEmailIndex).Assembly, store);
            return store;
        }
    }
}