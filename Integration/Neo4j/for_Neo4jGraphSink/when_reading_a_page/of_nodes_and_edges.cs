// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Graphs;
using static Cratis.Chronicle.Storage.Neo4j.Integration.GraphScenarioRun;
using context = Cratis.Chronicle.Neo4j.Integration.for_Neo4jGraphSink.when_reading_a_page.of_nodes_and_edges.context;

namespace Cratis.Chronicle.Neo4j.Integration.for_Neo4jGraphSink.when_reading_a_page;

/// <summary>
/// Reading a page of the nodes or the edges of a type counts all of them, and takes the page in the order of their keys.
/// </summary>
/// <param name="context">The context the facts assert against.</param>
[Collection(Neo4jCollection.Name)]
public class of_nodes_and_edges(context context) : Neo4jGiven<context>(context)
{
    public class context(Neo4jFixture fixture) : given.a_graph_sink(fixture)
    {
        public GraphElements<GraphNode> FirstPage = default!;
        public GraphElements<GraphNode> SecondPage = default!;
        public GraphElements<GraphNode> Beyond = default!;
        public GraphElements<GraphEdge> Edges = default!;
        public GraphElements<GraphEdge> VisitedEdges = default!;

        async Task Because()
        {
            await _run.Apply(
                1,
                new UpsertNode(Person("p1"), Props(("personId", "p1"), ("name", "Ada"))),
                new UpsertNode(Person("p2"), Props(("personId", "p2"), ("name", "Grace"))),
                new UpsertNode(Person("p3"), Props(("personId", "p3"), ("name", "Edsger"))),
                new UpsertEdge(MemberOf("p1", "g1"), Props()),
                new UpsertEdge(MemberOf("p2", "g1"), Props()),
                new UpsertEdge(Visited("p1", "g1", "v1"), Props(("count", 1))));
            FirstPage = await _sink.GetNodes("Person", 0, 2);
            SecondPage = await _sink.GetNodes("Person", 2, 2);
            Beyond = await _sink.GetNodes("Person", 10, 2);
            Edges = await _sink.GetEdges("MEMBER_OF");
            VisitedEdges = await _sink.GetEdges("VISITED");
        }
    }

    [Fact] void should_count_every_node_regardless_of_the_page() => Context.FirstPage.TotalCount.ShouldEqual(3L);
    [Fact] void should_take_the_size_of_the_page() => Context.FirstPage.Elements.Count.ShouldEqual(2);
    [Fact] void should_order_the_nodes_by_their_key() => Context.FirstPage.Elements.Select(_ => GraphValues.TextOf(_.Reference.Key)).ShouldContainOnly("p1", "p2");
    [Fact] void should_skip_to_the_next_page() => Context.SecondPage.Elements.Select(_ => GraphValues.TextOf(_.Reference.Key)).ShouldContainOnly("p3");
    [Fact] void should_return_an_empty_page_beyond_the_end() => Context.Beyond.Elements.ShouldBeEmpty();
    [Fact] void should_still_count_beyond_the_end() => Context.Beyond.TotalCount.ShouldEqual(3L);
    [Fact] void should_not_count_the_nodes_of_other_labels() => Context.Edges.Elements.Count.ShouldEqual(2);
    [Fact] void should_count_the_edges() => Context.Edges.TotalCount.ShouldEqual(2L);
    [Fact] void should_name_the_ends_of_an_edge() => Context.Edges.Elements.Select(_ => $"{GraphValues.TextOf(_.Reference.Source.Key)}->{GraphValues.TextOf(_.Reference.Target.Key)}").ShouldContainOnly("p1->g1", "p2->g1");
    [Fact] void should_give_an_identified_edge_its_identity() => GraphValues.TextOf(Context.VisitedEdges.Elements.Single().Reference.Identity).ShouldEqual("v1");
}
