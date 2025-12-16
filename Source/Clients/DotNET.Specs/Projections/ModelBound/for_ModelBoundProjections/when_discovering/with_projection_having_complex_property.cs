// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Projections;

namespace Cratis.Chronicle.Projections.ModelBound.for_ModelBoundProjections.when_discovering;

public class with_projection_having_complex_property : given.a_model_bound_projections
{
    IDictionary<Type, ProjectionDefinition> _result;

    void Establish()
    {
        _clientArtifactsProvider.ModelBoundProjections.Returns(
        [
            typeof(ProjectionWithComplexProperty),
            typeof(Address)
        ]);
    }

    void Because() => _result = projections.Discover();

    [Fact] void should_only_include_projection_with_complex_property() => _result.Keys.ShouldContainOnly([typeof(ProjectionWithComplexProperty)]);
    [Fact] void should_not_include_address_type() => _result.Keys.ShouldNotContain(typeof(Address));
}
