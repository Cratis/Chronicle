// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Diagnostics.Load.for_KernelLoad;

public class when_the_status_is_read_again_within_a_second : given.a_kernel_load
{
    KernelLoadStatus _status;

    void Because()
    {
        _ = _load.Status;
        _load.BeginPending("IProjectionsManager", "the-store");
        _time.Advance(TimeSpan.FromSeconds(0.5));
        _status = _load.Status;
    }

    [Fact] void should_not_evaluate_again() => _status.ShouldEqual(KernelLoadStatus.Accepting);
}
