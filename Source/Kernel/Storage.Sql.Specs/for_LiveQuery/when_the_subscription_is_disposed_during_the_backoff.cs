// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.for_LiveQuery;

public class when_the_subscription_is_disposed_during_the_backoff : given.a_live_query
{
    void Establish() => _script.Enqueue(() => throw new InvalidOperationException("database unavailable"));

    async Task Because()
    {
        Subscribe();
        await _time.WaitForTimers(1);
        _subscription.Dispose();
        _time.Advance(TimeSpan.FromHours(1));
    }

    [Fact] void should_not_poll_again() => _calls.ShouldEqual(1);
    [Fact] void should_release_the_backoff_timer() => _time.Timers.Single().IsDisposed.ShouldBeTrue();
    [Fact] void should_not_report_an_error() => _error.ShouldBeNull();
    [Fact] void should_count_only_the_failed_poll() => _failures.ShouldEqual(1);
}
