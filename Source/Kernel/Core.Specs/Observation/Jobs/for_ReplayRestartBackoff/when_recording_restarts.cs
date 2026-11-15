// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.Jobs.for_ReplayRestartBackoff;

public class when_recording_restarts : Specification
{
    static readonly DateTimeOffset _now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
    ReplayRestartBackoff _backoff;
    bool _dueAfterFirst;
    bool _dueRightAfterSecond;
    bool _dueAfterTheDelay;
    TimeSpan _delayAfterTenth;

    void Establish() => _backoff = new();

    void Because()
    {
        _backoff.Record(_now);
        _dueAfterFirst = _backoff.IsDue(_now);
        _backoff.Record(_now);
        _dueRightAfterSecond = _backoff.IsDue(_now);
        _dueAfterTheDelay = _backoff.IsDue(_now + ReplayRestartBackoff.InitialDelay);
        for (var restart = 3; restart <= 10; restart++)
        {
            _backoff.Record(_now);
        }

        _delayAfterTenth = _backoff.NextDelay;
    }

    [Fact] void should_allow_the_first_restart_straight_away() => _dueAfterFirst.ShouldBeTrue();
    [Fact] void should_hold_back_the_restart_after_the_second() => _dueRightAfterSecond.ShouldBeFalse();
    [Fact] void should_allow_it_once_the_delay_has_passed() => _dueAfterTheDelay.ShouldBeTrue();
    [Fact] void should_never_delay_longer_than_the_maximum() => _delayAfterTenth.ShouldEqual(ReplayRestartBackoff.MaxDelay);
}
