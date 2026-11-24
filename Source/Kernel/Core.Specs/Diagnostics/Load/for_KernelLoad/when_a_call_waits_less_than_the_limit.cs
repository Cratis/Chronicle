// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Diagnostics.Load.for_KernelLoad;

public class when_a_call_waits_less_than_the_limit : given.a_kernel_load
{
    void Because()
    {
        _load.BeginPending("IProjectionsManager", "the-store");
        _time.Advance(TimeSpan.FromSeconds(14));
    }

    [Fact] void should_be_accepting() => _load.Status.ShouldEqual(KernelLoadStatus.Accepting);
}
