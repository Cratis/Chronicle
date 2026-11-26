// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences.for_AppendedEventsQueue.when_an_event_arrives_for_an_unsubscribed_instance;

/// <summary>
/// The event is the doorbell, not the data: the observer is rung and reads from its own cursor.
/// </summary>
public class and_it_has_not_been_rung : given.a_queue_with_an_unsubscribed_instance
{
    bool _rungResult;

    async Task Because()
    {
        await Deliver(EventAt(10));
        _rungResult = await WasRung();
    }

    [Fact] void should_ring_the_observer() => _rungResult.ShouldBeTrue();
    [Fact] void should_not_hand_the_event_to_the_observer() => _calls.ShouldNotContain($"handle:{_observerKey.ObserverId}");
}
