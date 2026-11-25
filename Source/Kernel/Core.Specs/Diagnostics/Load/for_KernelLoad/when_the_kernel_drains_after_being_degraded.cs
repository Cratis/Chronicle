// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Diagnostics.Load.for_KernelLoad;

public class when_the_kernel_drains_after_being_degraded : given.a_kernel_load
{
    KernelLoadStatus _whileSaturated;
    KernelLoadStatus _justAfterDraining;
    KernelLoadStatus _afterTheRecoveryPeriod;
    PendingCall _call;

    void Establish()
    {
        _call = _load.BeginPending("IProjectionsManager", "the-store");
        _time.Advance(TimeSpan.FromSeconds(16));
    }

    void Because()
    {
        _whileSaturated = _load.Status;
        _load.EndPending(_call);
        _time.Advance(TimeSpan.FromSeconds(2));
        _justAfterDraining = _load.Status;
        _time.Advance(TimeSpan.FromSeconds(31));
        _afterTheRecoveryPeriod = _load.Status;
    }

    [Fact] void should_have_been_degraded_while_saturated() => _whileSaturated.ShouldEqual(KernelLoadStatus.Degraded);
    [Fact] void should_be_recovering_right_after_draining() => _justAfterDraining.ShouldEqual(KernelLoadStatus.Recovering);
    [Fact] void should_be_accepting_once_the_recovery_period_has_passed() => _afterTheRecoveryPeriod.ShouldEqual(KernelLoadStatus.Accepting);
}
