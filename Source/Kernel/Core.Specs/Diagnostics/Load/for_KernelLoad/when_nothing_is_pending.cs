// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Diagnostics.Load.for_KernelLoad;

public class when_nothing_is_pending : given.a_kernel_load
{
    KernelLoadReport _report;

    void Because() => _report = _load.Report();

    [Fact] void should_be_accepting() => _report.Status.ShouldEqual(KernelLoadStatus.Accepting);
    [Fact] void should_give_no_reasons() => _report.Reasons.ShouldBeEmpty();
    [Fact] void should_report_no_grains() => _report.Grains.ShouldBeEmpty();
    [Fact] void should_report_no_open_streams() => _report.OpenStreams.ShouldEqual(0);
}
