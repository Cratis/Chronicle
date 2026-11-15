// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Graphs;
using static Cratis.Chronicle.Storage.Neo4j.Integration.GraphScenarioRun;
using context = Cratis.Chronicle.Neo4j.Integration.for_Neo4jGraphSink.when_setting_properties.on_a_node.context;

namespace Cratis.Chronicle.Neo4j.Integration.for_Neo4jGraphSink.when_setting_properties;

/// <summary>
/// Setting properties on a node merges them into the node, and a null removes a property.
/// </summary>
/// <param name="context">The context the facts assert against.</param>
[Collection(Neo4jCollection.Name)]
public class on_a_node(context context) : Neo4jGiven<context>(context)
{
    public class context(Neo4jFixture fixture) : given.a_graph_sink(fixture)
    {
        public IReadOnlyDictionary<string, object> Stored = default!;

        async Task Because()
        {
            await _run.Apply(1, new UpsertNode(Person("p1"), Props(("personId", "p1"), ("name", "Ada"), ("visits", 1))));
            await _run.Apply(2, new SetProperties(Person("p1"), Props(("visits", 2), ("name", null))));
            Stored = (await StoredPerson("p1"))!;
        }
    }

    [Fact] void should_set_the_property() => Context.Stored["visits"].ShouldEqual(2L);
    [Fact] void should_remove_a_property_that_is_set_to_null() => Context.Stored.ContainsKey("name").ShouldBeFalse();
    [Fact] void should_move_the_watermark() => Context.Stored["_watermark"].ShouldEqual(2L);
}
