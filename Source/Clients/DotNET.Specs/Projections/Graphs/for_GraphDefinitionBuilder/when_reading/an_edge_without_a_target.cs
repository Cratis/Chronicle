// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Graphs.for_GraphDefinitionBuilder.when_reading;

public class an_edge_without_a_target : given.a_graph_definition_builder
{
    [Edge("DANGLING")]
    public record Dangling([Source<given.ModelBound.Person>] Guid PersonId);

    Exception _error;

    void Because() => _error = Catch.Exception(() => new ModelBoundGraphReader(_namingPolicy).ReadEdge(typeof(Dangling)));

    [Fact] void should_refuse_the_edge() => _error.ShouldBeOfExactType<EdgeMustHaveOneSourceAndOneTarget>();
}
