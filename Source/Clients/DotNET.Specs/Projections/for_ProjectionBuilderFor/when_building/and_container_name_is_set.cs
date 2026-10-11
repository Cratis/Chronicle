// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.for_ProjectionBuilderFor.when_building;

public class and_container_name_is_set : given.a_projection_builder
{
    Contracts.Projections.ProjectionDefinition _result;

    void Because()
    {
        builder.ContainerName("custom-container");
        _result = builder.Build();
    }

    [Fact] void should_keep_the_read_model_identifier() => _result.ReadModel.ShouldEqual(typeof(ProjectionReadModel).FullName);
    [Fact] void should_expose_the_container_name_override() => builder.ContainerNameOverride.ShouldEqual("custom-container");
}
