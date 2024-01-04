// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Kernel.Projections;

namespace Aksio.Cratis.Kernel.Grains.Projections.for_ProjectionManager;

public class when_getting_non_registered_projection : given.a_projection_manager_with_one_registered_projection
{
    Exception exception;

    void Because() => exception = Catch.Exception(() => manager.Get(Guid.NewGuid()));

    [Fact] void should_throw_missing_projection() => exception.ShouldBeOfExactType<MissingProjection>();
}
