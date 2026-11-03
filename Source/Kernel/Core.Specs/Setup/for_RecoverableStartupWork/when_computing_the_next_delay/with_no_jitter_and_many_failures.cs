// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Orleans.Hosting.for_RecoverableStartupWork.when_computing_the_next_delay;

public class with_no_jitter_and_many_failures : given.a_recoverable_startup_work
{
    TimeSpan _first;
    TimeSpan _capped;

    void Establish() => _jitter = 0;

    void Because()
    {
        _first = _work.NextDelay(1);
        _capped = _work.NextDelay(50);
    }

    [Fact] void should_wait_half_the_base_delay_first() => _first.ShouldEqual(RecoverableStartupWork.BaseDelay / 2);
    [Fact] void should_wait_half_the_cap_at_most_when_capped() => _capped.ShouldEqual(RecoverableStartupWork.MaxDelay / 2);
}
