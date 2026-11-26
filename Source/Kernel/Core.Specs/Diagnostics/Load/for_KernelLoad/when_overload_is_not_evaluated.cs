// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;

namespace Cratis.Chronicle.Diagnostics.Load.for_KernelLoad;

public class when_overload_is_not_evaluated : given.a_kernel_load
{
    protected override Overload Limits => new() { Enabled = false, MaxPendingCalls = 1 };

    void Because()
    {
        _load.BeginPending("IObserver", "the-store");
        _load.BeginPending("IObserver", "the-store");
    }

    [Fact] void should_always_be_accepting() => _load.Status.ShouldEqual(KernelLoadStatus.Accepting);
    [Fact] void should_still_count_the_calls() => _load.Report().Grains.Single().Pending.ShouldEqual(2);
}
