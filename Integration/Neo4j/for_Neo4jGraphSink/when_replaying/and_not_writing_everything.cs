// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Graphs;
using Neo4j.Driver;
using static Cratis.Chronicle.Storage.Neo4j.Integration.GraphScenarioRun;
using context = Cratis.Chronicle.Neo4j.Integration.for_Neo4jGraphSink.when_replaying.and_not_writing_everything.context;

namespace Cratis.Chronicle.Neo4j.Integration.for_Neo4jGraphSink.when_replaying;

/// <summary>
/// A replay writes the graph again. What it writes starts the element over, what it does not write is swept when the replay ends, and what other projections own is left alone.
/// </summary>
/// <param name="context">The context the facts assert against.</param>
[Collection(Neo4jCollection.Name)]
public class and_not_writing_everything(context context) : Neo4jGiven<context>(context)
{
    public class context(Neo4jFixture fixture) : given.a_graph_sink(fixture)
    {
        public GraphNode? StaleWhileReplaying;
        public GraphNode? RewrittenWhileReplaying;
        public IReadOnlyDictionary<string, object>? Person1;
        public IReadOnlyDictionary<string, object>? Person2;
        public IReadOnlyDictionary<string, object>? Person3;
        public IReadOnlyDictionary<string, object>? Group1;
        public IReadOnlyDictionary<string, object>? OtherOwnersPerson;
        public IReadOnlyList<IReadOnlyDictionary<string, object>> MemberOf1 = default!;
        public IReadOnlyList<IReadOnlyDictionary<string, object>> Visited1 = default!;
        public IReadOnlyDictionary<string, object>? OwnerRecord;

        async Task Because()
        {
            await _run.Apply(
                10,
                new UpsertNode(Person("p1"), Props(("personId", "p1"), ("name", "Before"))),
                new UpsertNode(Person("p2"), Props(("personId", "p2"), ("name", "Stale"))),
                new UpsertNode(Person("p3"), Props(("personId", "p3"), ("name", "Stale"))),
                new UpsertEdge(MemberOf("p1", "g1"), Props()),
                new UpsertEdge(Visited("p1", "g1", "v1"), Props(("count", 1))));
            await _run.Apply(OtherOwner, 11, new UpsertNode(Person("o1"), Props(("personId", "o1"), ("name", "Other"))));

            var replay = ReplayStartedAt(20);
            await _sink.BeginReplay(Owner, replay);
            StaleWhileReplaying = await _sink.FindNode(Owner, Person("p2"));
            await _run.Apply(2, new UpsertNode(Person("p1"), Props(("personId", "p1"), ("name", "Replayed"))), new UpsertEdge(MemberOf("p1", "g1"), Props()));
            RewrittenWhileReplaying = await _sink.FindNode(Owner, Person("p1"));
            await _sink.EndReplay(Owner, replay);

            Person1 = await StoredPerson("p1");
            Person2 = await StoredPerson("p2");
            Person3 = await StoredPerson("p3");
            Group1 = await StoredGroup("g1");
            OtherOwnersPerson = await StoredPerson("o1");
            MemberOf1 = await StoredEdges("MEMBER_OF");
            Visited1 = await StoredEdges("VISITED");
            OwnerRecord = (await Read($"MATCH (o:`_Owner` {{`_owner`: $owner}}){NamespaceFilter("o")} RETURN properties(o) AS properties", Parameters(null, ("owner", "owner"))))
                .Select(_ => _["properties"].As<IReadOnlyDictionary<string, object>>()).SingleOrDefault();
        }
    }

    [Fact] void should_read_what_was_not_written_yet_as_absent_while_replaying() => Context.StaleWhileReplaying.ShouldBeNull();
    [Fact] void should_read_what_was_written_as_present_while_replaying() => Context.RewrittenWhileReplaying.ShouldNotBeNull();
    [Fact] void should_accept_a_write_below_the_old_watermark_because_the_element_started_over() => Context.Person1!["name"].ShouldEqual("Replayed");
    [Fact] void should_give_the_written_node_the_sequence_number_of_the_replay() => Context.Person1!["_watermark"].ShouldEqual(2L);
    [Fact] void should_sweep_a_node_that_was_not_written() => Context.Person2.ShouldBeNull();
    [Fact] void should_sweep_every_node_that_was_not_written() => Context.Person3.ShouldBeNull();
    [Fact] void should_keep_the_edge_that_was_written() => Context.MemberOf1.Count.ShouldEqual(1);
    [Fact] void should_sweep_the_edge_that_was_not_written() => Context.Visited1.ShouldBeEmpty();
    [Fact] void should_keep_a_node_an_edge_created() => Context.Group1.ShouldNotBeNull();
    [Fact] void should_leave_what_another_projection_owns() => Context.OtherOwnersPerson.ShouldNotBeNull();
    [Fact] void should_record_the_generation_of_the_replay_for_the_owner() => ((long)Context.OwnerRecord!["_generation"]).ShouldEqual((long)Context.Person1!["_generation"]);
    [Fact] void should_move_the_generation_beyond_the_one_before_the_replay() => ((long)Context.Person1!["_generation"]).ShouldBeGreaterThan(0L);
}
