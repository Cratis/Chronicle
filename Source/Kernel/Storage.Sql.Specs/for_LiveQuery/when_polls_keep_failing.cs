// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.for_LiveQuery;

public class when_polls_keep_failing : given.a_live_query
{
    readonly TimeSpan[] _doubledDelays = [TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(4), TimeSpan.FromSeconds(8), TimeSpan.FromSeconds(16)];
    readonly TimeSpan[] _cappedDelays = [TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(30)];

    void Establish() => _script.Enqueue(() => throw new InvalidOperationException("database unavailable"));

    async Task Because()
    {
        Subscribe();
        for (var attempt = 1; attempt <= 6; attempt++)
        {
            await _time.WaitForTimers(attempt);
            if (attempt < 6)
            {
                _time.Advance(_time.RequestedDelays[attempt - 1]);
            }
        }
    }

    [Fact] void should_double_the_delay_after_each_failure() => _time.RequestedDelays.Take(4).ShouldEqual(_doubledDelays);
    [Fact] void should_cap_the_delay() => _time.RequestedDelays.Skip(4).ShouldEqual(_cappedDelays);
    [Fact] void should_keep_the_subscription_open() => (_error is null && !_completed).ShouldBeTrue();
    [Fact] void should_count_every_failed_poll() => _failures.ShouldEqual(6);
    [Fact] void should_poll_once_per_attempt() => _calls.ShouldEqual(6);
}
