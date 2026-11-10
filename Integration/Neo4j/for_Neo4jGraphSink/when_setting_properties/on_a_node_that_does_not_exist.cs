// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Graphs;
using static Cratis.Chronicle.Storage.Neo4j.Integration.GraphScenarioRun;
using context = Cratis.Chronicle.Neo4j.Integration.for_Neo4jGraphSink.when_setting_properties.on_a_node_that_does_not_exist.context;

namespace Cratis.Chronicle.Neo4j.Integration.for_Neo4jGraphSink.when_setting_properties;

/// <summary>
/// Setting properties never creates a node.
/// </summary>
/// <param name="context">The context the facts assert against.</param>
[Collection(Neo4jCollection.Name)]
public class on_a_node_that_does_not_exist(context context) : Neo4jGiven<context>(context)
{
    public class context(Neo4jFixture fixture) : given.a_graph_sink(fixture)
    {
        public long Nodes;
        public GraphNode Found = default!;

        async Task Because()
        {
            await _run.Apply(1, new SetProperties(Person("p1"), Props(("name", "Ada"))));
            Nodes = await CountNodes("Person");
            Found = (await _sink.FindNode(Owner, Person("p1")))!;
        }
    }

    [Fact] void should_not_create_the_node() => Context.Nodes.ShouldEqual(0L);
    [Fact] void should_not_find_the_node() => Context.Found.ShouldBeNull();
}
