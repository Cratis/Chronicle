// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;

namespace Cratis.Chronicle.Federation.for_FederationEnrollmentRateLimiter;

public class when_a_flood_of_sources_fills_the_table_while_one_is_throttled : Specification
{
    given.ControllableTimeProvider _time;
    FederationEnrollmentRateLimiter _limiter;
    bool _throttledSourceAfterTheFlood;

    static IPAddress Source(int index) => new([10, (byte)(index >> 16), (byte)(index >> 8), (byte)index]);

    void Establish()
    {
        _time = new();
        _limiter = new(_time);
    }

    void Because()
    {
        // The source that is being throttled is the one seen longest ago, which is what evicting the oldest first would drop.
        var flooder = IPAddress.Parse("203.0.113.10");
        for (var i = 0; i < 20; i++)
        {
            _limiter.TryAcquire(flooder, 5);
        }

        for (var i = 1; i <= 10_100; i++)
        {
            _time.Advance(TimeSpan.FromMilliseconds(1));
            _limiter.TryAcquire(Source(i), 5);
        }

        _throttledSourceAfterTheFlood = _limiter.TryAcquire(flooder, 5);
    }

    [Fact] void should_keep_throttling_the_source_that_was_over_its_budget() => _throttledSourceAfterTheFlood.ShouldBeFalse();
}
