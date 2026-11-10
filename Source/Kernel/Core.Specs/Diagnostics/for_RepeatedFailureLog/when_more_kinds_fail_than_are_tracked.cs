// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Diagnostics.for_RepeatedFailureLog.given;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Diagnostics.for_RepeatedFailureLog;

public class when_more_kinds_fail_than_are_tracked : Specification
{
    const int MaxKinds = 4;

    ManualTime _time;
    RepeatedFailureLog _log;
    int _writtenOverflow;
    IReadOnlyList<SuppressedFailures> _ended;
    IReadOnlyList<SuppressedFailures> _endedAfterForgetting;
    int _writtenAfterForgetting;

    void Establish()
    {
        _time = new();
        _log = new(TimeSpan.FromSeconds(10), 1, _time, MaxKinds);
    }

    void Because()
    {
        for (var i = 0; i < MaxKinds; i++)
        {
            _log.ShouldWrite("Category", $"Failure {i}", LogLevel.Error, new TimeoutException());
        }

        for (var i = MaxKinds; i < 100; i++)
        {
            if (_log.ShouldWrite("Category", $"Failure {i}", LogLevel.Error, new TimeoutException()))
            {
                _writtenOverflow++;
            }
        }

        _time.Advance(TimeSpan.FromSeconds(11));
        _ended = _log.TakeEnded();
        _endedAfterForgetting = _log.TakeEnded();

        for (var i = 100; i < 100 + MaxKinds; i++)
        {
            if (_log.ShouldWrite("Category", $"Failure {i}", LogLevel.Error, new TimeoutException()))
            {
                _writtenAfterForgetting++;
            }
        }
    }

    [Fact] void should_write_the_first_failure_beyond_the_bound() => _writtenOverflow.ShouldEqual(1);
    [Fact] void should_count_the_rest_under_the_overflow_kind() => _ended.Single().ShouldEqual(new SuppressedFailures(RepeatedFailureLog.OverflowCategory, string.Empty, typeof(Exception), 100 - MaxKinds - 1));
    [Fact] void should_have_nothing_more_to_report() => _endedAfterForgetting.ShouldBeEmpty();
    [Fact] void should_track_new_kinds_once_the_old_ones_are_forgotten() => _writtenAfterForgetting.ShouldEqual(MaxKinds);
}
