// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Graphs.for_GraphDefinitionBuilder.when_reading;

public class a_fluent_edge_identified_by_a_property : given.a_graph_definition_builder
{
    [Edge("VISITED")]
    public record Visit([Source<given.Fluent.Person>] Guid PersonId, [Target<given.Fluent.Place>] Guid PlaceId, Guid VisitId);

    GraphEdgeModel _edge;

    void Because()
    {
        var builder = new GraphEdgeBuilder<Visit, given.Fluent.Person, given.Fluent.Place>(_namingPolicy);
        builder.IdentifiedBy(v => v.VisitId);
        _edge = builder.Build();
    }

    [Fact] void should_use_the_declared_identifying_property() => _edge.IdentifyingProperty.ShouldEqual("visitId");
    [Fact] void should_exclude_the_end_node_keys() => _edge.ExcludedProperties.ShouldContainOnly("personId", "placeId");
}
