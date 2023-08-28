// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Kernel.Engines.Projections.for_ProjectionManager;

public class when_getting_registered_projection : given.a_projection_manager_with_one_registered_projection
{
    IProjection result;

    void Because() => result = manager.Get(projection_definition.Identifier);

    [Fact] void should_return_projection() => result.ShouldNotBeNull();
}
