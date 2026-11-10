// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Graphs;
using static Cratis.Chronicle.Storage.Neo4j.Integration.GraphScenarioRun;
using context = Cratis.Chronicle.Neo4j.Integration.for_Neo4jGraphSink.when_replaying.and_leaving_without_ending.context;

namespace Cratis.Chronicle.Neo4j.Integration.for_Neo4jGraphSink.when_replaying;

/// <summary>
/// A replay that is left without ending sweeps nothing, and the graph reads as it did before.
/// </summary>
/// <param name="context">The context the facts assert against.</param>
[Collection(Neo4jCollection.Name)]
public class and_leaving_without_ending(context context) : Neo4jGiven<context>(context)
{
    public class context(Neo4jFixture fixture) : given.a_graph_sink(fixture)
    {
        public IReadOnlyDictionary<string, object>? Person2;
        public GraphNode? FoundAfterLeaving;

        async Task Because()
        {
            await _run.Apply(10, new UpsertNode(Person("p1"), Props(("personId", "p1"))), new UpsertNode(Person("p2"), Props(("personId", "p2"), ("name", "Kept"))));
            await _sink.BeginReplay(Owner, ReplayStartedAt(20));
            await _run.Apply(2, new UpsertNode(Person("p1"), Props(("personId", "p1"), ("name", "Replayed"))));
            await _sink.LeaveReplay(Owner);
            Person2 = await StoredPerson("p2");
            FoundAfterLeaving = await _sink.FindNode(Owner, Person("p2"));
        }
    }

    [Fact] void should_not_sweep_anything() => Context.Person2.ShouldNotBeNull();
    [Fact] void should_read_the_elements_as_present_again() => Context.FoundAfterLeaving.ShouldNotBeNull();
}
