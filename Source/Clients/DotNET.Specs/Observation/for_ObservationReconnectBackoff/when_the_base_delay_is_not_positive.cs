// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.for_ObservationReconnectBackoff;

public class when_the_base_delay_is_not_positive : Specification
{
    TimeSpan _zero;
    TimeSpan _infinite;

    void Because()
    {
        var zero = new ObservationReconnectBackoff(TimeSpan.Zero, TimeSpan.FromSeconds(60), TimeSpan.FromSeconds(30), TimeProvider.System, () => 1);
        zero.NextDelay();
        _zero = zero.NextDelay();
        var infinite = new ObservationReconnectBackoff(Timeout.InfiniteTimeSpan, TimeSpan.FromSeconds(60), TimeSpan.FromSeconds(30), TimeProvider.System, () => 1);
        _infinite = infinite.NextDelay();
    }

    [Fact] void should_not_back_off_from_zero() => _zero.ShouldEqual(TimeSpan.Zero);
    [Fact] void should_keep_an_infinite_delay() => _infinite.ShouldEqual(Timeout.InfiniteTimeSpan);
}
