// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Graphs;
using Cratis.Chronicle.Storage.Neo4j.Integration;
using static Cratis.Chronicle.Storage.Neo4j.Integration.GraphScenarioRun;
using context = Cratis.Chronicle.Neo4j.Integration.for_Neo4jGraphSink.when_isolating_namespaces.with_the_same_keys_in_two_namespaces.context;

namespace Cratis.Chronicle.Neo4j.Integration.for_Neo4jGraphSink.when_isolating_namespaces;

/// <summary>
/// Two namespaces with the same keys keep their own graphs: a node of the same key in each, a delete or a sweep in one that never reaches the other, and a database of their own or the namespace stamped on everything, as the database mode places them.
/// </summary>
/// <param name="context">The context the facts assert against.</param>
[Collection(Neo4jCollection.Name)]
public class with_the_same_keys_in_two_namespaces(context context) : Neo4jGiven<context>(context)
{
    public class context(Neo4jFixture fixture) : given.a_graph_sink(fixture)
    {
        public string Other = NewNamespace();
        public string FirstDatabase = default!;
        public string SecondDatabase = default!;
        public IReadOnlyDictionary<string, object> FirstP1 = default!;
        public IReadOnlyDictionary<string, object> SecondP1 = default!;
        public IReadOnlyDictionary<string, object>? FirstP3AfterTheSweep;
        public IReadOnlyDictionary<string, object>? SecondP3AfterTheSweep;
        public IReadOnlyDictionary<string, object>? FirstP1AfterTheDelete;
        public IReadOnlyDictionary<string, object>? SecondP1AfterTheDelete;
        public GraphElements<GraphNode> FirstPeople = default!;
        public GraphElements<GraphNode> SecondPeople = default!;
        public IReadOnlyList<string> ServerDatabases = default!;

        async Task Because()
        {
            var secondSink = CreateSink(null, Other);
            await secondSink.EnsureSchema();
            var second = new GraphScenarioRun(secondSink);

            await _run.Apply(1, new UpsertNode(Person("p1"), Props(("personId", "p1"), ("name", "First"))), new UpsertNode(Person("p3"), Props(("personId", "p3"), ("name", "First"))));
            await second.Apply(2, new UpsertNode(Person("p1"), Props(("personId", "p1"), ("name", "Second"))), new UpsertNode(Person("p3"), Props(("personId", "p3"), ("name", "Second"))), new UpsertEdge(MemberOf("p1", "g1"), Props()));
            FirstP1 = (await StoredPerson("p1"))!;
            SecondP1 = (await StoredPerson("p1", Other))!;
            FirstPeople = await _sink.GetNodes("Person");
            SecondPeople = await secondSink.GetNodes("Person");

            await second.Apply(3, new DeleteNode(Person("p1"), true));
            FirstP1AfterTheDelete = await StoredPerson("p1");
            SecondP1AfterTheDelete = await StoredPerson("p1", Other);

            var replay = ReplayStartedAt(20);
            await secondSink.BeginReplay(Owner, replay);
            await second.Apply(4, new UpsertNode(Person("p2"), Props(("personId", "p2"))));
            await secondSink.EndReplay(Owner, replay);
            FirstP3AfterTheSweep = await StoredPerson("p3");
            SecondP3AfterTheSweep = await StoredPerson("p3", Other);

            FirstDatabase = DatabaseOf();
            SecondDatabase = DatabaseOf(Other);
            ServerDatabases = await Databases();
        }
    }

    [Fact] void should_keep_the_node_of_the_first_namespace() => Context.FirstP1["name"].ShouldEqual("First");
    [Fact] void should_keep_a_node_with_the_same_key_in_the_second_namespace() => Context.SecondP1["name"].ShouldEqual("Second");
    [Fact] void should_count_only_the_nodes_of_the_namespace_in_the_first() => Context.FirstPeople.TotalCount.ShouldEqual(2L);
    [Fact] void should_count_only_the_nodes_of_the_namespace_in_the_second() => Context.SecondPeople.TotalCount.ShouldEqual(2L);
    [Fact] void should_not_let_a_delete_in_one_namespace_reach_the_other() => (Context.FirstP1AfterTheDelete is not null && Context.SecondP1AfterTheDelete is null).ShouldBeTrue();
    [Fact] void should_not_let_a_sweep_in_one_namespace_reach_the_other() => (Context.FirstP3AfterTheSweep is not null && Context.SecondP3AfterTheSweep is null).ShouldBeTrue();
    [Fact] void should_stamp_the_namespace_when_the_namespaces_share_a_database() => Context.FirstP1.ContainsKey("_namespace").ShouldEqual(Context.IsSingleDatabase);
    [Fact] void should_stamp_the_namespace_of_the_node() => (!Context.IsSingleDatabase || (string)Context.SecondP1["_namespace"] == Context.Other).ShouldBeTrue();
    [Fact] void should_use_one_database_when_the_namespaces_share_it_and_one_each_otherwise() => (Context.FirstDatabase == Context.SecondDatabase).ShouldEqual(Context.IsSingleDatabase);
    [Fact] void should_have_the_database_of_every_namespace_on_the_server() => (Context.ServerDatabases.Contains(Context.FirstDatabase) && Context.ServerDatabases.Contains(Context.SecondDatabase)).ShouldBeTrue();
}
