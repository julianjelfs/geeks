using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using NUnit.Framework;
using Raven.Client;
using Raven.Client.Document;
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
        public void LoadStartingWith()
        {
            var user = _session.Query<User>()
                .Include<User>(u=>u.Friends.Select(f=>f.UserId))
                .SingleOrDefault(u => u.Name == "julian");

            foreach (var friend in user.Friends)
            {
                Console.WriteLine("FriendName: {0}", _session.Load<User>(friend.UserId).Name);
            }
        }

        [Test]
        public void CustomIndex()
        {
            var users = _session.Query<User>("FriendNameAndEmailIndex")
                                .SingleOrDefault(user => user.Username == "julian.jelfs@googlemail.com");

            foreach (var friend in users.Friends)
            {
                Console.WriteLine("Name: {0}", _session.Load<User>(friend.UserId));
            }
        }

        [Test]
        public void whatever()
        {
            using(var bi = _store.BulkInsert())
            for (var i = 0; i < 100; i++)
            {
                bi.Store(new User
                    {
                        Id = i.ToString(),
                        Name = string.Format("Mr {0}", i)
                    });
            }

            var user = new User
                {
                    Id = "julian",
                    Name = "julian"
                };
            for (var i = 0; i < 100; i++)
            {
                user.Friends.Add(new Friend
                    {
                        UserId = i.ToString()
                    });
            }

            _session.Store(user);
            _session.SaveChanges();
        }

    }
}
