// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Projections;

namespace Cratis.Chronicle.Projections.ModelBound.for_ModelBoundProjections.when_discovering;

public class with_multiple_independent_projections : given.a_model_bound_projections
{
    IDictionary<Type, ProjectionDefinition> _result;

    void Establish()
    {
        _clientArtifactsProvider.ModelBoundProjections.Returns([typeof(ParentProjection), typeof(ChildProjection)]);
    }

    void Because() => _result = projections.Discover();

    [Fact] void should_include_first_projection() => _result.Keys.ShouldContain(typeof(ParentProjection));
    [Fact] void should_include_second_projection() => _result.Keys.ShouldContain(typeof(ChildProjection));
    [Fact] void should_have_two_projections() => _result.Count.ShouldEqual(2);
}
