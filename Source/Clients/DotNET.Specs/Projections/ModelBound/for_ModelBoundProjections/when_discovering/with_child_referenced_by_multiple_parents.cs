// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Projections;

namespace Cratis.Chronicle.Projections.ModelBound.for_ModelBoundProjections.when_discovering;

public class with_child_referenced_by_multiple_parents : given.a_model_bound_projections
{
    IDictionary<Type, ProjectionDefinition> _result;

    void Establish()
    {
        _clientArtifactsProvider.ModelBoundProjections.Returns([
            typeof(ParentWithChildren),
            typeof(AnotherParentWithChildren),
            typeof(ChildProjection)
        ]);
    }

    void Because() => _result = projections.Discover();

    [Fact] void should_include_first_parent_projection() => _result.Keys.ShouldContain(typeof(ParentWithChildren));
    [Fact] void should_include_second_parent_projection() => _result.Keys.ShouldContain(typeof(AnotherParentWithChildren));
    [Fact] void should_not_include_child_projection() => _result.Keys.ShouldNotContain(typeof(ChildProjection));
    [Fact] void should_have_two_projections() => _result.Count.ShouldEqual(2);
}
