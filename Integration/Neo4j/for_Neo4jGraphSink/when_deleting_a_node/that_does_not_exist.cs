// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Graphs;
using static Cratis.Chronicle.Storage.Neo4j.Integration.GraphScenarioRun;
using context = Cratis.Chronicle.Neo4j.Integration.for_Neo4jGraphSink.when_deleting_a_node.that_does_not_exist.context;

namespace Cratis.Chronicle.Neo4j.Integration.for_Neo4jGraphSink.when_deleting_a_node;

/// <summary>
/// Deleting a node that was never written is not an error.
/// </summary>
/// <param name="context">The context the facts assert against.</param>
[Collection(Neo4jCollection.Name)]
public class that_does_not_exist(context context) : Neo4jGiven<context>(context)
{
    public class context(Neo4jFixture fixture) : given.a_graph_sink(fixture)
    {
        public Exception? Error;

        async Task Because() => Error = await Catch.Exception(() => _run.Apply(1, new DeleteNode(Person("nobody"), false), new DeleteNode(Person("nobody"), true)));
    }

    [Fact] void should_not_fail() => Context.Error.ShouldBeNull();
}
