// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Diagnostics.for_RepeatedFailureLog.given;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Diagnostics.for_RepeatedFailureLog;

public class when_the_same_failure_repeats : Specification
{
    ManualTime _time;
    RepeatedFailureLog _log;
    bool _firstWritten;
    int _written;
    IReadOnlyList<SuppressedFailures> _endedTooEarly;
    IReadOnlyList<SuppressedFailures> _ended;
    bool _writtenInNextWindow;

    void Establish()
    {
        _time = new();
        _log = new(TimeSpan.FromSeconds(10), RepeatedFailureLog.DefaultAllowedPerWindow, _time);
    }

    void Because()
    {
        _firstWritten = _log.ShouldWrite("Category", "Reading {Grain} failed", LogLevel.Error, new TimeoutException("a", new InvalidOperationException()));
        for (var i = 0; i < 99; i++)
        {
            if (_log.ShouldWrite("Category", "Reading {Grain} failed", LogLevel.Error, new TimeoutException("a", new InvalidOperationException())))
            {
                _written++;
            }
        }

        _endedTooEarly = _log.TakeEnded();
        _time.Advance(TimeSpan.FromSeconds(11));
        _ended = _log.TakeEnded();
        _writtenInNextWindow = _log.ShouldWrite("Category", "Reading {Grain} failed", LogLevel.Error, new TimeoutException("b", new InvalidOperationException()));
    }

    [Fact] void should_write_the_first_occurrence() => _firstWritten.ShouldBeTrue();
    [Fact] void should_not_write_the_repeats() => _written.ShouldEqual(0);
    [Fact] void should_not_report_before_the_window_ends() => _endedTooEarly.ShouldBeEmpty();
    [Fact] void should_report_the_count_when_the_window_ends() => _ended.Single().Suppressed.ShouldEqual(99);
    [Fact] void should_report_the_message() => _ended.Single().Message.ShouldEqual("Reading {Grain} failed");
    [Fact] void should_count_by_the_root_exception() => _ended.Single().Exception.ShouldEqual(typeof(InvalidOperationException));
    [Fact] void should_write_again_in_the_next_window() => _writtenInNextWindow.ShouldBeTrue();
}
