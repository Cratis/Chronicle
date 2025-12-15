// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Projections;

namespace Cratis.Chronicle.Projections.ModelBound.for_ModelBoundProjections.when_discovering;

public class with_nested_children_hierarchy : given.a_model_bound_projections
{
    IDictionary<Type, ProjectionDefinition> _result;

    void Establish()
    {
        _clientArtifactsProvider.ModelBoundProjections.Returns([
            typeof(ParentWithNestedChildren),
            typeof(NestedChildProjection),
            typeof(ChildProjection)
        ]);
    }

    void Because() => _result = projections.Discover();

    [Fact] void should_only_include_root_parent_projection() => _result.Keys.ShouldContainOnly([typeof(ParentWithNestedChildren)]);
    [Fact] void should_not_include_nested_child_projection() => _result.Keys.ShouldNotContain(typeof(NestedChildProjection));
    [Fact] void should_not_include_leaf_child_projection() => _result.Keys.ShouldNotContain(typeof(ChildProjection));
}
