// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Graphs.for_GraphDefinitionBuilder.when_reading;

public class a_node_without_a_key : given.a_graph_definition_builder
{
    [Node]
    public record Keyless(Guid Id, string Name);

    Exception _error;

    void Because() => _error = Catch.Exception(() => new ModelBoundGraphReader(_namingPolicy).ReadNode(typeof(Keyless)));

    [Fact] void should_refuse_the_node() => _error.ShouldBeOfExactType<NodeMustHaveExactlyOneKey>();
}
