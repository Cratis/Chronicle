// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Graphs;
using Cratis.Chronicle.Storage.Neo4j;
using Cratis.Chronicle.Storage.Neo4j.Integration;
using Neo4j.Driver;
using static Cratis.Chronicle.Storage.Neo4j.Integration.GraphScenarioRun;
using context = Cratis.Chronicle.Neo4j.Integration.for_Neo4jGraphSink.when_creating_the_database.for_a_namespace.context;

namespace Cratis.Chronicle.Neo4j.Integration.for_Neo4jGraphSink.when_creating_the_database;

/// <summary>
/// With a database per namespace the sink creates the database of an event store and a namespace when the schema is ensured, and with one shared database it creates none.
/// </summary>
/// <param name="context">The context the facts assert against.</param>
[Collection(Neo4jCollection.Name)]
public class for_a_namespace(context context) : Neo4jGiven<context>(context)
{
    public class context(Neo4jFixture fixture) : Neo4jSpecification(fixture)
    {
        public IReadOnlyList<string> Before = default!;
        public IReadOnlyList<string> After = default!;
        public string Expected = default!;
        public IReadOnlyList<IRecord> Stored = default!;

        async Task Because()
        {
            Expected = GraphDatabaseName.For(null, EventStore, Namespace);
            Before = await Databases();
            var sink = CreateSink();
            await sink.EnsureSchema();
            await new GraphScenarioRun(sink).Apply(1, new UpsertNode(Person("p1"), Props(("personId", "p1"))));
            After = await Databases();
            Stored = await Read("MATCH (n:Person) RETURN n.personId AS key");
        }
    }

    [Fact] void should_not_have_the_database_before() => Context.Before.Contains(Context.Expected).ShouldBeFalse();
    [Fact] void should_create_the_database_of_the_event_store_and_the_namespace_when_every_namespace_has_one() => Context.After.Contains(Context.Expected).ShouldEqual(!Context.IsSingleDatabase);
    [Fact] void should_create_no_database_when_the_namespaces_share_one() => Context.After.Order().SequenceEqual(Context.Before.Order()).ShouldEqual(Context.IsSingleDatabase);
    [Fact] void should_name_the_database_after_the_event_store_and_the_namespace() => Context.Expected.ShouldEqual($"{Neo4jSpecification.EventStore}-{Context.Namespace}");
    [Fact] void should_write_into_the_database_of_the_namespace() => Context.Stored.Count.ShouldEqual(1);
}
