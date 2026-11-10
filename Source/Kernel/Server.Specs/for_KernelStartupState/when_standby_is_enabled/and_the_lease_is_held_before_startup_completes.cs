// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Server.for_KernelStartupState.when_standby_is_enabled;

public class and_the_lease_is_held_before_startup_completes : given.a_kernel_startup_state
{
    bool _standby;
    bool _ready;

    void Establish()
    {
        _options.Clustering.SingleWriterLease.StandbyStartupTimeout = TimeSpan.FromMinutes(10);
        _lease.IsHeld.Returns(true);
        _lease.HeldFor.Returns(TimeSpan.FromMinutes(9));
    }

    void Because() => (_standby, _ready) = (_state.IsStandby, _state.IsReady);

    [Fact] void should_stay_in_standby_until_startup_completes() => _standby.ShouldBeTrue();
    [Fact] void should_not_be_ready_to_serve() => _ready.ShouldBeFalse();
}
