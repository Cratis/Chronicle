// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Kernel.Grains.Projections.for_ProjectionManager;

public class when_asking_if_non_registered_projection_exists : given.a_projection_manager_without_any_projections
{
    bool result;

    void Because() => result = manager.Exists(Guid.NewGuid());

    [Fact] void should_return_false() => result.ShouldBeFalse();
}
