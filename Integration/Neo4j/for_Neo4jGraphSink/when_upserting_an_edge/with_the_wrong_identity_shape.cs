// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Graphs;
using static Cratis.Chronicle.Storage.Neo4j.Integration.GraphScenarioRun;
using context = Cratis.Chronicle.Neo4j.Integration.for_Neo4jGraphSink.when_upserting_an_edge.with_the_wrong_identity_shape.context;

namespace Cratis.Chronicle.Neo4j.Integration.for_Neo4jGraphSink.when_upserting_an_edge;

/// <summary>
/// An edge that does not fit its definition is refused, and nothing of the changeset it was part of is written.
/// </summary>
/// <param name="context">The context the facts assert against.</param>
[Collection(Neo4jCollection.Name)]
public class with_the_wrong_identity_shape(context context) : Neo4jGiven<context>(context)
{
    public class context(Neo4jFixture fixture) : given.a_graph_sink(fixture)
    {
        public Exception? WithoutIdentity;
        public Exception? WithIdentityOnAnEdgeThatHasNone;
        public Exception? BetweenTheWrongLabels;
        public long Nodes;

        async Task Because()
        {
            WithoutIdentity = await Catch.Exception(() => _run.Apply(1, new UpsertNode(Person("p1"), Props()), new UpsertEdge(new("VISITED", Person("p1"), Group("g1")), Props())));
            WithIdentityOnAnEdgeThatHasNone = await Catch.Exception(() => _run.Apply(1, new UpsertEdge(new("MEMBER_OF", Person("p1"), Group("g1"), "x"), Props())));
            BetweenTheWrongLabels = await Catch.Exception(() => _run.Apply(1, new UpsertEdge(new("MEMBER_OF", Group("g1"), Person("p1")), Props())));
            Nodes = await CountNodes("Person") + await CountNodes("Group");
        }
    }

    [Fact] void should_refuse_an_edge_that_needs_an_identity_and_has_none() => Context.WithoutIdentity.ShouldBeOfExactType<InvalidOperationException>();
    [Fact] void should_refuse_an_identity_on_an_edge_that_has_no_identifying_property() => Context.WithIdentityOnAnEdgeThatHasNone.ShouldBeOfExactType<InvalidOperationException>();
    [Fact] void should_refuse_an_edge_between_the_wrong_labels() => Context.BetweenTheWrongLabels.ShouldBeOfExactType<InvalidOperationException>();
    [Fact] void should_write_nothing_of_a_changeset_that_is_refused() => Context.Nodes.ShouldEqual(0L);
}
