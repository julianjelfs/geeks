using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using NUnit.Framework;
using Raven.Client;
using Raven.Client.Document;
using Raven.Client.Linq;
using geeks.Indexes;
using geeks.Models;

namespace geeks.tests
{
    [TestFixture]
    public class RavenTests
    {
        private IDocumentStore _store;
        private IDocumentSession _session;

        [TestFixtureSetUp]
        public void SetupFixture()
        {
            _store = new DocumentStore
                {
                    ConnectionStringName = "LocalRavenDB"
                }.Initialize();
        }

        [TestFixtureTearDown]
        public void TeardownFixture()
        {
            _store.Dispose();
        }

        [SetUp]
        public void Setup()
        {
            _session = _store.OpenSession();
        }

        [TearDown]
        public void Teardown()
        {
            //_session.SaveChanges();
        }


        [Test]
        public void UsersByEmail()
        {
            var emails = new HashSet<string>
                {
                    "james.kane@gmail.com",
                    "kallina.jelfs@gmail.com",
                    "trotmanrog@gmail.com"
                };

            var users = _session.Query<User>()
                                .Where(u => u.Username.In(emails));

            foreach (var user in users)
            {
                Console.WriteLine("User: {0}", user);
            }
        }
    }
}
