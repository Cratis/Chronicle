// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Orleans.Hosting.for_RecoverableStartupWork.when_computing_the_next_delay;

public class with_full_jitter_and_growing_failures : given.a_recoverable_startup_work
{
    TimeSpan _first;
    TimeSpan _fourth;
    TimeSpan _capped;

    void Establish() => _jitter = 1;

    void Because()
    {
        _first = _work.NextDelay(1);
        _fourth = _work.NextDelay(4);
        _capped = _work.NextDelay(50);
    }

    [Fact] void should_wait_the_base_delay_first() => _first.ShouldEqual(RecoverableStartupWork.BaseDelay);
    [Fact] void should_double_for_each_failure() => _fourth.ShouldEqual(RecoverableStartupWork.BaseDelay * 8);
    [Fact] void should_never_exceed_the_cap() => _capped.ShouldEqual(RecoverableStartupWork.MaxDelay);
}
