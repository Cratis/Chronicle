// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Diagnostics.Load.for_KernelLoad;

public class when_reporting_what_the_kernel_is_busy_with : given.a_kernel_load
{
    KernelLoadReport _report;

    void Because()
    {
        _load.BeginPending("IObserver", "store-a");
        _time.Advance(TimeSpan.FromSeconds(5));
        _load.BeginPending("IProjectionsManager", "store-b");
        _load.BeginRunning("IObserver", "store-a");
        _load.BeginRunning("IObserver", "store-a");
        _load.EndRunning("IObserver", "store-a");
        _load.StreamOpened();
        _load.StreamOpened();
        _load.StreamClosed();
        _time.Advance(TimeSpan.FromSeconds(1));
        _report = _load.Report(busiest: 1);
    }

    [Fact] void should_list_the_busiest_grain_first() => _report.Grains.Single().GrainType.ShouldEqual("IObserver");
    [Fact] void should_report_the_oldest_pending_call_age() => _report.Grains.Single().OldestPendingAge.ShouldEqual(TimeSpan.FromSeconds(6));
    [Fact] void should_report_the_calls_still_running() => _report.Grains.Single().Running.ShouldEqual(1);
    [Fact] void should_report_the_open_streams() => _report.OpenStreams.ShouldEqual(1);
    [Fact] void should_limit_the_report_to_the_busiest() => _report.Grains.Count.ShouldEqual(1);
}
