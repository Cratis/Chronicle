// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Diagnostics.for_RepeatedFailureLog.given;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Diagnostics.for_RepeatedFailureLog;

public class when_taking_windows_that_are_still_running : Specification
{
    ManualTime _time;
    RepeatedFailureLog _log;
    IReadOnlyList<SuppressedFailures> _ended;
    IReadOnlyList<SuppressedFailures> _all;
    IReadOnlyList<SuppressedFailures> _allAgain;

    void Establish()
    {
        _time = new();
        _log = new(TimeSpan.FromSeconds(10), 1, _time);
    }

    void Because()
    {
        _log.ShouldWrite("Category", "Reading {Grain} failed", LogLevel.Warning, new TimeoutException());
        for (var i = 0; i < 5; i++)
        {
            _log.ShouldWrite("Category", "Reading {Grain} failed", i == 2 ? LogLevel.Critical : LogLevel.Warning, new TimeoutException());
        }

        _time.Advance(TimeSpan.FromSeconds(3));
        _ended = _log.TakeEnded();
        _all = _log.TakeEnded(includeRunningWindows: true);
        _allAgain = _log.TakeEnded(includeRunningWindows: true);
    }

    [Fact] void should_not_report_the_running_window_by_default() => _ended.ShouldBeEmpty();
    [Fact] void should_report_the_count_of_the_running_window() => _all.Single().Suppressed.ShouldEqual(5);
    [Fact] void should_report_the_highest_level_counted() => _all.Single().Level.ShouldEqual(LogLevel.Critical);
    [Fact] void should_report_each_count_only_once() => _allAgain.ShouldBeEmpty();
}
