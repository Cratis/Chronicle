// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.for_AbandonedDeliveryDrain;

public class when_the_drain_call_times_out_once : given.a_captured_delivery
{
    void Establish() => _subscriber.Drain().Returns(Task.FromException(new TimeoutException()), Task.CompletedTask);

    async Task Because()
    {
        var drain = Drain();
        _clock.Advance(TimeSpan.FromSeconds(1));
        await drain;
    }

    [Fact] void should_retry_the_original_barrier() => _subscriber.Received(2).Drain();
}
