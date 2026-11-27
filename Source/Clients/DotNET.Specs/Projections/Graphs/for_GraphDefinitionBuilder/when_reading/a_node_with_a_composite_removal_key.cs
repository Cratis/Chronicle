// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Keys;
using Cratis.Chronicle.Projections.ModelBound;

namespace Cratis.Chronicle.Projections.Graphs.for_GraphDefinitionBuilder.when_reading;

public class a_node_with_a_composite_removal_key : given.a_graph_definition_builder
{
    public record ProjectRemoved(string Owner, string Repository);

    [Node]
    [RemovedWith<ProjectRemoved>(CompositeKey = new[] { nameof(ProjectRemoved.Owner), nameof(ProjectRemoved.Repository) })]
    public record Project([Key] string Id);

    Exception _error;

    void Because() => _error = Catch.Exception(() => new ModelBoundGraphReader(_namingPolicy).ReadNode(typeof(Project)));

    [Fact] void should_refuse_the_unsupported_removal_key() => _error.ShouldBeOfExactType<NotSupportedException>();
}
