// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Graphs;
using Cratis.Chronicle.Storage.Neo4j.Integration;
using static Cratis.Chronicle.Storage.Neo4j.Integration.GraphScenarioRun;
using context = Cratis.Chronicle.Neo4j.Integration.for_GraphDatabaseName.when_used_by_the_sink.for_namespaces_that_need_sanitizing.context;

namespace Cratis.Chronicle.Neo4j.Integration.for_GraphDatabaseName.when_used_by_the_sink;

/// <summary>
/// The names of the databases the algorithm gives work on a real server, also for a namespace that has characters a database name cannot have, and for the default namespace.
/// </summary>
/// <param name="context">The context the facts assert against.</param>
[Collection(Neo4jCollection.Name)]
public class for_namespaces_that_need_sanitizing(context context) : Neo4jGiven<context>(context)
{
    public class context(Neo4jFixture fixture) : given.a_graph_sink(fixture)
    {
        public string Odd = $"Acme Corp {Guid.NewGuid():N}"[..20];
        public string OddDatabase = default!;
        public IReadOnlyList<string> ServerDatabases = default!;
        public IReadOnlyDictionary<string, object> StoredOdd = default!;
        public IReadOnlyDictionary<string, object> StoredDefault = default!;
        public string DefaultDatabase = default!;

        async Task Because()
        {
            var odd = CreateSink(null, Odd);
            var @default = CreateSink(null, "Default");
            await odd.EnsureSchema();
            await @default.EnsureSchema();
            await new GraphScenarioRun(odd).Apply(1, new UpsertNode(Person("p1"), Props(("personId", "p1"))));
            await new GraphScenarioRun(@default).Apply(1, new UpsertNode(Person("p1"), Props(("personId", "p1"))));
            OddDatabase = DatabaseOf(Odd);
            DefaultDatabase = DatabaseOf("Default");
            ServerDatabases = await Databases();
            StoredOdd = (await StoredPerson("p1", Odd))!;
            StoredDefault = (await StoredPerson("p1", "Default"))!;
        }
    }

    [Fact] void should_get_a_name_the_server_accepts() => Context.StoredOdd.ShouldNotBeNull();
    [Fact] void should_have_that_database_on_the_server() => Context.ServerDatabases.Contains(Context.OddDatabase).ShouldBeTrue();
    [Fact] void should_sanitize_and_hash_the_name_when_every_namespace_has_a_database() => (Context.IsSingleDatabase || (Context.OddDatabase.StartsWith("specs-acme-corp-", StringComparison.Ordinal) && Context.OddDatabase.Length == 35 && Context.OddDatabase[26] == '-')).ShouldBeTrue();
    [Fact] void should_keep_the_namespace_as_it_is_when_the_namespaces_share_the_database() => (!Context.IsSingleDatabase || (string)Context.StoredOdd["_namespace"] == Context.Odd).ShouldBeTrue();
    [Fact] void should_add_nothing_for_the_default_namespace() => Context.DefaultDatabase.ShouldEqual(Context.IsSingleDatabase ? "neo4j" : Neo4jSpecification.EventStore);
    [Fact] void should_write_the_default_namespace_into_that_database() => Context.StoredDefault.ShouldNotBeNull();
}
