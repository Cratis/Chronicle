// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences.for_AppendedEventsQueue.when_an_event_arrives_for_an_unsubscribed_instance;

/// <summary>
/// An observer that stays unsubscribed after a ring, such as one whose client is gone, is not rung again for every event.
/// </summary>
public class and_it_was_rung_moments_ago : given.a_queue_with_an_unsubscribed_instance
{
    async Task Establish()
    {
        await Deliver(EventAt(10));
        await WasRung();
    }

    async Task Because()
    {
        _clock.Advance(TimeSpan.FromSeconds(10));
        await Deliver(EventAt(11));
    }

    [Fact] void should_ring_it_only_once() => _calls.Count(_ => _ == $"ring:{_observerKey.ObserverId}").ShouldEqual(1);
}
