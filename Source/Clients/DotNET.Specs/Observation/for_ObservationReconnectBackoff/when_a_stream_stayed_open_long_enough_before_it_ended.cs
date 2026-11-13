// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.for_ObservationReconnectBackoff;

public class when_a_stream_stayed_open_long_enough_before_it_ended : given.a_backoff
{
    TimeSpan _afterAShortStream;
    TimeSpan _afterALongStream;

    void Establish()
    {
        for (var stream = 0; stream < 4; stream++) StreamEndsAtOnce();
    }

    void Because()
    {
        _backoff.StreamOpened();
        _time.Advance(_healthyStreamDuration - TimeSpan.FromSeconds(1));
        _afterAShortStream = _backoff.NextDelay();

        _backoff.StreamOpened();
        _time.Advance(_healthyStreamDuration);
        _afterALongStream = _backoff.NextDelay();
    }

    [Fact] void should_keep_backing_off_after_a_stream_that_ended_early() => _afterAShortStream.ShouldEqual(TimeSpan.FromSeconds(32));
    [Fact] void should_start_over_at_the_base_delay_after_a_long_lived_stream() => _afterALongStream.ShouldEqual(_baseDelay);
}
