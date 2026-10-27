// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.for_Observer.when_handling;

public class and_the_live_subscriber_wait_is_clamped : Specification
{
    TimeSpan[] _timeouts;

    void Because() => _timeouts = new[] { 30, 10, 0 }
        .Select(seconds => Observer.ClampLiveSubscriberTimeout(TimeSpan.FromSeconds(seconds), TimeSpan.FromSeconds(30)))
        .ToArray();

    [Fact] void should_leave_five_seconds_of_response_headroom() => _timeouts[0].ShouldEqual(TimeSpan.FromSeconds(25));
    [Fact] void should_keep_a_shorter_subscriber_timeout() => _timeouts[1].ShouldEqual(TimeSpan.FromSeconds(10));
    [Fact] void should_also_bound_an_indefinite_subscriber_timeout() => _timeouts[2].ShouldEqual(TimeSpan.FromSeconds(25));
}
