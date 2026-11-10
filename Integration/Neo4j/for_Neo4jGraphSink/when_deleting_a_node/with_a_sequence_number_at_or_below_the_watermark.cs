// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Graphs;
using static Cratis.Chronicle.Storage.Neo4j.Integration.GraphScenarioRun;
using context = Cratis.Chronicle.Neo4j.Integration.for_Neo4jGraphSink.when_deleting_a_node.with_a_sequence_number_at_or_below_the_watermark.context;

namespace Cratis.Chronicle.Neo4j.Integration.for_Neo4jGraphSink.when_deleting_a_node;

/// <summary>
/// A delete by an event at or below the watermark of the node changes nothing.
/// </summary>
/// <param name="context">The context the facts assert against.</param>
[Collection(Neo4jCollection.Name)]
public class with_a_sequence_number_at_or_below_the_watermark(context context) : Neo4jGiven<context>(context)
{
    public class context(Neo4jFixture fixture) : given.a_graph_sink(fixture)
    {
        public IReadOnlyDictionary<string, object>? Stored;

        async Task Because()
        {
            await _run.Apply(5, new UpsertNode(Person("p1"), Props(("personId", "p1"), ("name", "Ada"))));
            await _run.Apply(5, new DeleteNode(Person("p1"), true));
            await _run.Apply(4, new DeleteNode(Person("p1"), true));
            Stored = await StoredPerson("p1");
        }
    }

    [Fact] void should_keep_the_node() => Context.Stored.ShouldNotBeNull();
}
