// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Projections;

namespace Cratis.Chronicle.Projections.ModelBound.for_ModelBoundProjectionBuilder;

public class when_building_from_type_without_attribute : given.a_model_bound_projection_builder
{
    ProjectionDefinition? _result;

    void Because() => _result = builder.Build(typeof(string));

    [Fact] void should_return_null() => _result.ShouldBeNull();
}
