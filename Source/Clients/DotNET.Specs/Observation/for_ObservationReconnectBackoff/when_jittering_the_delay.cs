// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.for_ObservationReconnectBackoff;

public class when_jittering_the_delay : given.a_backoff
{
    TimeSpan _lowest;
    TimeSpan _middle;

    void Because()
    {
        _jitter = 0;
        _lowest = StreamEndsAtOnce();
        _jitter = 0.5;
        _middle = StreamEndsAtOnce();
    }

    [Fact] void should_wait_at_least_half_of_the_backed_off_delay() => _lowest.ShouldEqual(TimeSpan.FromSeconds(1));
    [Fact] void should_spread_within_the_upper_half_of_the_backed_off_delay() => _middle.ShouldEqual(TimeSpan.FromSeconds(3));
}
