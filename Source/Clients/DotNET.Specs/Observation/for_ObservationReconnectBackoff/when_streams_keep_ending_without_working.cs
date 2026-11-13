// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.for_ObservationReconnectBackoff;

public class when_streams_keep_ending_without_working : given.a_backoff
{
    readonly List<TimeSpan> _delays = [];

    void Because()
    {
        for (var stream = 0; stream < 8; stream++) _delays.Add(StreamEndsAtOnce());
    }

    [Fact] void should_start_at_the_base_delay() => _delays[0].ShouldEqual(_baseDelay);
    [Fact] void should_double_while_streams_keep_ending() => _delays[1..5].ShouldContainOnly(TimeSpan.FromSeconds(4), TimeSpan.FromSeconds(8), TimeSpan.FromSeconds(16), TimeSpan.FromSeconds(32));
    [Fact] void should_not_exceed_the_ceiling() => _delays[5..].ShouldContainOnly(_maxDelay, _maxDelay, _maxDelay);
}
