// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Diagnostics.Load.for_KernelLoad;

public class when_too_many_calls_are_pending : given.a_kernel_load
{
    void Because()
    {
        for (var call = 0; call < 4; call++)
        {
            _load.BeginPending("IObserver", "the-store");
        }
    }

    [Fact] void should_be_degraded() => _load.Status.ShouldEqual(KernelLoadStatus.Degraded);
    [Fact] void should_say_how_many_are_waiting() => _load.Reasons.Single().ShouldContain("4 calls");
}
