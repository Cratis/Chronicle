// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Server.for_KernelStartupState.when_standby_is_enabled;

public class and_startup_has_completed : given.a_kernel_startup_state
{
    bool _standby;
    bool _ready;

    void Establish()
    {
        _lease.IsHeld.Returns(true);
        _started.Cancel();
    }

    void Because() => (_standby, _ready) = (_state.IsStandby, _state.IsReady);

    [Fact] void should_leave_standby() => _standby.ShouldBeFalse();
    [Fact] void should_be_ready_to_serve() => _ready.ShouldBeTrue();
}
