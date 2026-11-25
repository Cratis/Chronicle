// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Diagnostics.Load.for_KernelLoad;

public class when_the_oldest_call_waits_too_long : given.a_kernel_load
{
    KernelLoadReport _report;

    void Because()
    {
        _load.BeginPending("IProjectionsManager", "the-store");
        _time.Advance(TimeSpan.FromSeconds(16));
        _report = _load.Report();
    }

    [Fact] void should_be_degraded() => _report.Status.ShouldEqual(KernelLoadStatus.Degraded);
    [Fact] void should_name_the_grain_in_the_reason() => _report.Reasons.Single().ShouldContain("IProjectionsManager");
    [Fact] void should_name_the_event_store_in_the_reason() => _report.Reasons.Single().ShouldContain("the-store");
    [Fact] void should_report_how_long_it_has_waited() => _report.Grains.Single().OldestPendingAge.ShouldEqual(TimeSpan.FromSeconds(16));
    [Fact] void should_report_it_as_pending() => _report.Grains.Single().Pending.ShouldEqual(1);
}
