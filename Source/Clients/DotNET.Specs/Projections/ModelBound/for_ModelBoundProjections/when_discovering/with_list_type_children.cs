// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Projections;

namespace Cratis.Chronicle.Projections.ModelBound.for_ModelBoundProjections.when_discovering;

public class with_list_type_children : given.a_model_bound_projections
{
    IDictionary<Type, ProjectionDefinition> _result;

    void Establish()
    {
        _clientArtifactsProvider.ModelBoundProjections.Returns([typeof(ParentWithListChildren), typeof(ChildProjection)]);
    }

    void Because() => _result = projections.Discover();

    [Fact] void should_only_include_parent_projection() => _result.Keys.ShouldContainOnly([typeof(ParentWithListChildren)]);
    [Fact] void should_not_include_child_projection() => _result.Keys.ShouldNotContain(typeof(ChildProjection));
}
