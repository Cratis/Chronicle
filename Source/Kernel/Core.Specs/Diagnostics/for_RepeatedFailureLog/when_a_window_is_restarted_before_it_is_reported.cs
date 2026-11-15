// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Diagnostics.for_RepeatedFailureLog.given;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Diagnostics.for_RepeatedFailureLog;

public class when_a_window_is_restarted_before_it_is_reported : Specification
{
    ManualTime _time;
    RepeatedFailureLog _log;
    IReadOnlyList<SuppressedFailures> _ended;

    void Establish()
    {
        _time = new();
        _log = new(TimeSpan.FromSeconds(10), 1, _time);
    }

    void Because()
    {
        for (var i = 0; i < 5; i++)
        {
            _log.ShouldWrite("Category", LogLevel.Error, new TimeoutException());
        }

        _time.Advance(TimeSpan.FromSeconds(11));
        for (var i = 0; i < 3; i++)
        {
            _log.ShouldWrite("Category", LogLevel.Error, new TimeoutException());
        }

        _ended = _log.TakeEnded();
    }

    [Fact] void should_report_the_count_of_the_ended_window_without_the_running_one() => _ended.Single().Suppressed.ShouldEqual(4);
}
