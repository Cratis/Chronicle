// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Kernel.Grains.Projections.for_ProjectionManager;

public class when_asking_if_registered_projection_exists : given.a_projection_manager_with_one_registered_projection
{
    bool result;

    void Because() => result = manager.Exists(projection_definition.Identifier);

    [Fact] void should_return_true() => result.ShouldBeTrue();
}
