// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Federation.for_FederationConsumerTiming;

public class when_backing_off : Specification
{
    FederationConsumerTiming _timing;
    TimeSpan _first;
    TimeSpan _second;
    TimeSpan _third;
    TimeSpan _capped;

    void Establish() => _timing = new(TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(5), TimeSpan.FromMinutes(10), TimeSpan.FromSeconds(30));

    void Because()
    {
        _first = _timing.NextBackoff(TimeSpan.Zero);
        _second = _timing.NextBackoff(_first);
        _third = _timing.NextBackoff(_second);
        _capped = _timing.NextBackoff(_third);
    }

    [Fact] void should_start_at_the_initial_backoff() => _first.ShouldEqual(TimeSpan.FromSeconds(1));
    [Fact] void should_double() => _second.ShouldEqual(TimeSpan.FromSeconds(2));
    [Fact] void should_keep_doubling() => _third.ShouldEqual(TimeSpan.FromSeconds(4));
    [Fact] void should_stop_at_the_cap() => _capped.ShouldEqual(TimeSpan.FromSeconds(5));
}
