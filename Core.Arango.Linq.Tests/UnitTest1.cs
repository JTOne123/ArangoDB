using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Core.Arango.Protocol;
using Xunit;

namespace Core.Arango.Linq.Tests
{
    public class Project
    {
        public Guid Key { get; set; }
        public string Name { get; set; }
        public int Value { get; set; }
    }

    public class UnitTest1 : IAsyncLifetime
    {
        protected readonly ArangoContext Arango =
            new ArangoContext($"Server=http://localhost:8529;Realm=CI-{Guid.NewGuid():D};User=root;Password=;");

        [Fact]
        public async void TestToList()
        {
            var test = Arango.AsQueryable<Project>("test").ToList();
        }

        /// <summary>
        /// �berpr�ft die Funktionalit�t eines SingleOrDefault-Querys mit Einschr�nkung des Projektnamens
        /// expected query: FOR x IN Project FILTER x.Name == "A" return x
        /// </summary>
        [Fact]
        public async void TestSingleOrDefault()
        {
            var test = Arango.AsQueryable<Project>("test").SingleOrDefault(x => x.Name == "A");
            Assert.True(test.Name == "A");
        }

        /// <summary>
        /// expected query: FOR x IN Project FILTER x.Name == "A" return x.Name
        /// </summary>
        [Fact]
        public void TestWhereSelect()
        {
            var test = Arango.AsQueryable<Project>("test").Where(x => x.Name == "A").Select(x => x.Name).ToList();
            foreach (var t in test)
            {
                Assert.True(t  == "A");
            }
        }

        /// <summary>
        /// expected query: FOR x IN Project FILTER x.Value IN @list RETURN x
        /// </summary>
        [Fact]
        public void TestListContains()
        {
            var list = new List<int> { 1, 2, 3 };
            var test = Arango.AsQueryable<Project>("test").Where(x => list.Contains(x.Value)).ToList();
            foreach (var t in test)
            {
                Assert.Contains(t.Value, list);
            }
        }

        /// <summary>
        /// expected query: FOR x IN Project FILTER x.Value == 1 || x.Value == 2 RETURN x
        /// </summary>
        [Fact]
        public async void TestOr()
        {
            var test = Arango.AsQueryable<Project>("test").Where(x => x.Value == 1 || x.Value == 2).ToList();
            foreach (var t in test)
            {
                Assert.True(t.Value == 1 || t.Value == 2);
            }
        }

        /// <summary>
        /// expected query: FOR x IN Project FILTER x.Name LIKE "A%" RETURN x
        /// </summary>
        [Fact]
        public void TestStringBeginsWith()
        {
            var test = Arango.AsQueryable<Project>("test").Where(x => x.Name.StartsWith("A")).ToList();
            foreach (var t in test)
            {
                Assert.StartsWith("A", t.Name);
                Assert.True(test.Count > 0);
            }
        }

        /// <summary>
        /// expected query: FOR x IN Project FILTER x.Name == @p || x.Name == @pUnique RETURN x.Name
        /// </summary>
        [Fact]
        public async void TestInjection()
        {
            var test = Arango.AsQueryable<Project>("test").Where(x => x.Name == "A" || x.Name == "B \" RETURN 42").Select(x => x.Name).ToList();
            Assert.True(test.Count == 1);
        }

        /// <summary>
        /// �berpr�ft die Funktionalit�t eines SingleOrDefault-Querys mit Einschr�nkung der Guid
        /// expected query: FOR x IN Project FILTER x.Key == @testGuid return x
        /// </summary>
        [Fact]
        public async void TestSingleOrDefaultGuid()
        {
            var testGuid = Guid.NewGuid();

            await Arango.CreateDocumentAsync("test", nameof(Project), new Project
            {
                Key = testGuid,
                Name = "TestSingleOrDefault",
                Value = 2
            });

            var test = Arango.AsQueryable<Project>("test").SingleOrDefault(x => x.Key == testGuid);

            Assert.True(test.Key == testGuid);
        }
        
        /// <summary>
        /// Initialisiert eine Datenbank und eine Collection f�r die Tests
        /// </summary>
        /// <returns></returns>
        public async Task InitializeAsync()
        {
            await Arango.CreateDatabaseAsync("test");
            await Arango.CreateCollectionAsync("test", nameof(Project), ArangoCollectionType.Document);

            await Arango.CreateDocumentAsync("test", nameof(Project), new Project
            {
                Key = Guid.NewGuid(),
                Name = "A",
                Value = 1
            });
            await Arango.CreateDocumentAsync("test", nameof(Project), new Project
            {
                Key = Guid.NewGuid(),
                Name = "B",
                Value = 2
            });
            await Arango.CreateDocumentAsync("test", nameof(Project), new Project
            {
                Key = Guid.NewGuid(),
                Name = "C",
                Value = 3
            });
        }

        /// <summary>
        /// L�scht die angelegten Datenbanken
        /// </summary>
        /// <returns></returns>
        public async Task DisposeAsync()
        {
            try
            {
                foreach (var db in await Arango.ListDatabasesAsync())
                    await Arango.DropDatabaseAsync(db);
            }
            catch
            {
                //
            }
        }
    }
}