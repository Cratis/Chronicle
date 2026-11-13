// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.for_ObservationReconnectBackoff;

public class when_a_stream_delivered_before_it_ended : given.a_backoff
{
    TimeSpan _delay;

    void Establish()
    {
        for (var stream = 0; stream < 4; stream++) StreamEndsAtOnce();
    }

    void Because()
    {
        _backoff.StreamOpened();
        _backoff.StreamDelivered();
        _delay = _backoff.NextDelay();
    }

    [Fact] void should_start_over_at_the_base_delay() => _delay.ShouldEqual(_baseDelay);
}
