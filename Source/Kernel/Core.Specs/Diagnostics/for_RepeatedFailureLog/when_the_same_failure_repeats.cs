// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Diagnostics.for_RepeatedFailureLog;

public class when_the_same_failure_repeats : Specification
{
    ManualTime _time;
    RepeatedFailureLog _log;
    int _written;
    IReadOnlyList<(string Category, Type Exception, int Suppressed)> _endedTooEarly;
    IReadOnlyList<(string Category, Type Exception, int Suppressed)> _ended;
    bool _writtenInNextWindow;

    void Establish()
    {
        _time = new();
        _log = new(TimeSpan.FromSeconds(10), 3, _time);
    }

    void Because()
    {
        for (var i = 0; i < 100; i++)
        {
            if (_log.ShouldWrite("Category", LogLevel.Error, new TimeoutException("a", new InvalidOperationException())))
            {
                _written++;
            }
        }

        _endedTooEarly = _log.TakeEnded();
        _time.Advance(TimeSpan.FromSeconds(11));
        _ended = _log.TakeEnded();
        _writtenInNextWindow = _log.ShouldWrite("Category", LogLevel.Error, new TimeoutException("b", new InvalidOperationException()));
    }

    sealed class ManualTime : TimeProvider
    {
        DateTimeOffset _now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

        public override DateTimeOffset GetUtcNow() => _now;

        public void Advance(TimeSpan by) => _now += by;
    }

    [Fact] void should_write_only_the_allowed_number() => _written.ShouldEqual(3);
    [Fact] void should_not_report_before_the_window_ends() => _endedTooEarly.ShouldBeEmpty();
    [Fact] void should_report_the_count_when_the_window_ends() => _ended.Single().Suppressed.ShouldEqual(97);
    [Fact] void should_count_by_the_root_exception() => _ended.Single().Exception.ShouldEqual(typeof(InvalidOperationException));
    [Fact] void should_write_again_in_the_next_window() => _writtenInNextWindow.ShouldBeTrue();
}
