// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Server.for_KernelStartupState.when_standby_is_enabled;

public class and_the_lease_is_held_past_the_standby_startup_timeout : given.a_kernel_startup_state
{
    KernelStartupStatus _status;
    bool _standby;
    bool _ready;

    void Establish()
    {
        _options.Clustering.SingleWriterLease.StandbyStartupTimeout = TimeSpan.FromMinutes(10);
        _lease.IsHeld.Returns(true);
        _lease.HeldFor.Returns(TimeSpan.FromMinutes(10));
    }

    void Because() => (_status, _standby, _ready) = (_state.Status, _state.IsStandby, _state.IsReady);

    [Fact] void should_report_that_standby_has_expired() => _status.ShouldEqual(KernelStartupStatus.StandbyExpired);
    [Fact] void should_no_longer_be_in_standby() => _standby.ShouldBeFalse();
    [Fact] void should_not_be_ready_to_serve() => _ready.ShouldBeFalse();
}
