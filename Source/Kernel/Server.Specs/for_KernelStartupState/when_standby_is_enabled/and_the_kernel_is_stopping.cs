// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Server.for_KernelStartupState.when_standby_is_enabled;

public class and_the_kernel_is_stopping : given.a_kernel_startup_state
{
    bool _standby;

    void Establish()
    {
        _lease.IsWaiting.Returns(true);
        _stopping.Cancel();
    }

    void Because() => _standby = _state.IsStandby;

    [Fact] void should_leave_standby() => _standby.ShouldBeFalse();
}
