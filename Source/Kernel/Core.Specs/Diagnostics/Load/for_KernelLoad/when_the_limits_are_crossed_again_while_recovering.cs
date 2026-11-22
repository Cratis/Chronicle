// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Diagnostics.Load.for_KernelLoad;

public class when_the_limits_are_crossed_again_while_recovering : given.a_kernel_load
{
    KernelLoadStatus _status;

    void Establish()
    {
        var call = _load.BeginPending("IProjectionsManager", "the-store");
        _time.Advance(TimeSpan.FromSeconds(16));
        _ = _load.Status;
        _load.EndPending(call);
        _time.Advance(TimeSpan.FromSeconds(2));
        _ = _load.Status;
    }

    void Because()
    {
        _load.BeginPending("IProjectionsManager", "the-store");
        _time.Advance(TimeSpan.FromSeconds(16));
        _status = _load.Status;
    }

    [Fact] void should_be_degraded_again() => _status.ShouldEqual(KernelLoadStatus.Degraded);
}
