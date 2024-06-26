// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Projections.Pipelines;

namespace Cratis.Chronicle.Grains.Projections.for_ProjectionManager;

public class when_getting_registered_projection_pipeline : given.a_projection_manager_with_one_registered_projection
{
    IProjectionPipeline result;

    void Because() => result = manager.GetPipeline(projection_definition.Identifier);

    [Fact] void should_return_projection_pipeline() => result.ShouldNotBeNull();
}
