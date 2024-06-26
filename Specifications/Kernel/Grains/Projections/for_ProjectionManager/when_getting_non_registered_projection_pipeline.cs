// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Projections;

namespace Cratis.Chronicle.Grains.Projections.for_ProjectionManager;

public class when_getting_non_registered_projection_pipeline : given.a_projection_manager_with_one_registered_projection
{
    Exception exception;

    void Because() => exception = Catch.Exception(() => manager.GetPipeline(Guid.NewGuid()));

    [Fact] void should_throw_missing_projection() => exception.ShouldBeOfExactType<MissingProjection>();
}
