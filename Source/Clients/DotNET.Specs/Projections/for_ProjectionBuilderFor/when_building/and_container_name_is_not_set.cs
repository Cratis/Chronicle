// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.for_ProjectionBuilderFor.when_building;

public class and_container_name_is_not_set : given.a_projection_builder
{
    Contracts.Projections.ProjectionDefinition _result;

    void Because() => _result = builder.Build();

    [Fact] void should_use_the_full_name_as_read_model_identifier() => _result.ReadModel.ShouldEqual(typeof(ProjectionReadModel).FullName);
    [Fact] void should_not_have_a_container_name_override() => builder.ContainerNameOverride.ShouldBeNull();
}
