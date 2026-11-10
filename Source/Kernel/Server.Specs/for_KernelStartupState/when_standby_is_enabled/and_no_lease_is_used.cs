// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Server.for_KernelStartupState.when_standby_is_enabled;

public class and_no_lease_is_used : given.a_kernel_startup_state
{
    bool _standby;

    void Establish() => _state = Server.given.startup_state.For(_lifetime, _options);

    void Because() => _standby = _state.IsStandby;

    [Fact] void should_not_be_in_standby() => _standby.ShouldBeFalse();
}
