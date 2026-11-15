// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Keys;

namespace Cratis.Chronicle.Projections.Graphs.for_GraphDefinitionBuilder.when_building;

public class a_node_with_a_nested_object : given.a_graph_definition_builder
{
    public record Address(string Street);

    [Node]
    public record Resident([Key] Guid ResidentId, Address Home);

    Exception _error;

    void Because() => _error = Catch.Exception(() => _builder.Build(ModelBoundModelOf(typeof(Resident)), "residents", "Neo4j"));

    [Fact] void should_refuse_the_graph() => _error.ShouldBeOfExactType<NestedObjectInGraph>();
}
