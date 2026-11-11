// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.for_ObservationReconnectBackoff;

public class when_a_stream_ends_after_the_kernel_gave_up_establishing_it : given.a_backoff
{
    /// <summary>
    /// The kernel asks a busy observer again on the same stream: four attempts of a thirty-second grain call each.
    /// </summary>
    static readonly TimeSpan _kernelEstablishmentBudget = TimeSpan.FromSeconds(4 * 30);

    TimeSpan _afterTheFirstStream;
    TimeSpan _afterTheSecondStream;

    void Establish() => _backoff = new(_baseDelay, _maxDelay, ObservationReconnectBackoff.DefaultHealthyStreamDuration, _time, () => _jitter);

    void Because()
    {
        _backoff.StreamOpened();
        _time.Advance(_kernelEstablishmentBudget + TimeSpan.FromSeconds(5));
        _afterTheFirstStream = _backoff.NextDelay();

        _backoff.StreamOpened();
        _time.Advance(_kernelEstablishmentBudget + TimeSpan.FromSeconds(5));
        _afterTheSecondStream = _backoff.NextDelay();
    }

    [Fact] void should_count_a_stream_that_never_delivered_as_not_having_worked() => _afterTheFirstStream.ShouldEqual(_baseDelay);
    [Fact] void should_keep_backing_off() => _afterTheSecondStream.ShouldEqual(_baseDelay * 2);
}
