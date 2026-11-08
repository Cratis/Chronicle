// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Sinks;

namespace Cratis.Chronicle.Projections.Graphs.for_GraphProjections;

#pragma warning disable SA1402 // File may only contain a single type
public class when_discovering_graphs_that_name_sinks : Specification
{
    GraphProjections _graphs;

    void Establish() => _graphs = given.graph_projections.For(
        [
            typeof(for_GraphDefinitionBuilder.given.ModelBound.Person),
            typeof(for_GraphDefinitionBuilder.given.ModelBound.Group),
            typeof(for_GraphDefinitionBuilder.given.ModelBound.Place),
            typeof(for_GraphDefinitionBuilder.given.ModelBound.MemberOf),
            typeof(for_GraphDefinitionBuilder.given.ModelBound.Visited)
        ],
        [typeof(PeopleGraph)],
        "graphs");

    void Because() => _graphs.Discover();

    [Fact] void should_not_fail() => _graphs.Failures.ShouldBeEmpty();
    [Fact] void should_store_the_graph_naming_a_sink_there() => _graphs.Registrations.Single(_ => _.Projection.Identifier == typeof(PeopleGraph).FullName).ReadModel.Sink.Name.ShouldEqual("people");
    [Fact] void should_store_the_graph_naming_no_sink_in_the_default() => _graphs.Registrations.Single(_ => _.Projection.Identifier == "Organization").ReadModel.Sink.Name.ShouldEqual("graphs");
}

[Graph("People")]
[Sink("people")]
public class PeopleGraph : for_GraphDefinitionBuilder.given.Fluent.OrganizationGraph;
