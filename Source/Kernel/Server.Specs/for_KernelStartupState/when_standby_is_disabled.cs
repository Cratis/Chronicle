// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Server.for_KernelStartupState;

public class when_standby_is_disabled : given.a_kernel_startup_state
{
    bool _standby;

    void Establish()
    {
        _options.Clustering.SingleWriterLease.Standby = false;
        _lease.IsWaiting.Returns(true);
    }

    void Because() => _standby = _state.IsStandby;

    [Fact] void should_not_be_in_standby_while_waiting() => _standby.ShouldBeFalse();
}
