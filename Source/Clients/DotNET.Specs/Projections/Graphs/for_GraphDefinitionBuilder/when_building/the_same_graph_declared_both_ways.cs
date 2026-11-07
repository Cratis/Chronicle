// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Graphs.for_GraphDefinitionBuilder.when_building;

public class the_same_graph_declared_both_ways : given.a_graph_definition_builder
{
    GraphRegistration _modelBound;
    GraphRegistration _fluent;

    void Because()
    {
        _modelBound = _builder.Build(ModelBoundModel(), "organization", "Neo4j");
        _fluent = _builder.Build(FluentModel(), "organization", "Neo4j");
    }

    [Fact] void should_produce_identical_projection_graph_definitions() => CanonicalJson(_fluent.Projection.Graph!).ShouldEqual(CanonicalJson(_modelBound.Projection.Graph!));
    [Fact] void should_produce_identical_graph_schemas() => Canonical(System.Text.Json.Nodes.JsonNode.Parse(_fluent.ReadModel.Schema)).ToJsonString().ShouldEqual(Canonical(System.Text.Json.Nodes.JsonNode.Parse(_modelBound.ReadModel.Schema)).ToJsonString());
    [Fact] void should_produce_the_same_graph_name() => _fluent.GraphName.ShouldEqual(_modelBound.GraphName);
}
