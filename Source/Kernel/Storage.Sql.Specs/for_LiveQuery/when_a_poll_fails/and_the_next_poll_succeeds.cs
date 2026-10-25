// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Storage.Sql.for_LiveQuery.when_a_poll_fails;

public class and_the_next_poll_succeeds : given.a_live_query
{
    readonly InvalidOperationException _failure = new("database unavailable");

    void Establish()
    {
        _script.Enqueue(() => throw _failure);
        _script.Enqueue(() => [1]);
    }

    async Task Because()
    {
        Subscribe();
        await _time.WaitForTimers(1);
        _time.Advance(Interval);
        await _firstEmission.Task.WaitAsync(TimeSpan.FromSeconds(15), TimeProvider.System);
        await _time.WaitForTimers(2);
    }

    [Fact] void should_emit_the_result_of_the_next_poll() => _emissions.Single().ShouldEqual([1]);
    [Fact] void should_not_report_an_error() => _error.ShouldBeNull();
    [Fact] void should_not_complete() => _completed.ShouldBeFalse();
    [Fact] void should_wait_the_polling_interval_before_retrying() => _time.RequestedDelays[0].ShouldEqual(Interval);
    [Fact] void should_return_to_the_polling_interval_after_recovering() => _time.RequestedDelays[1].ShouldEqual(Interval);
    [Fact] void should_count_the_failed_poll() => _failures.ShouldEqual(1);
    [Fact] void should_log_the_failure_as_a_warning() => _logger.Entries.ShouldContain(entry => entry.Level == LogLevel.Warning && ReferenceEquals(entry.Error, _failure));
    [Fact] void should_log_the_recovery() => _logger.Entries.Count(entry => entry.Level == LogLevel.Information).ShouldEqual(1);
}
