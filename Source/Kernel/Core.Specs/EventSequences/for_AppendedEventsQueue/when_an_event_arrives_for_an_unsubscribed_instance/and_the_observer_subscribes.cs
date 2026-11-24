// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.EventSequences.for_AppendedEventsQueue.when_an_event_arrives_for_an_unsubscribed_instance;

/// <summary>
/// Once the rung observer takes its live subscription again, its events are delivered live and it is not rung.
/// </summary>
public class and_the_observer_subscribes : given.a_queue_with_an_unsubscribed_instance
{
    async Task Establish()
    {
        await Deliver(EventAt(10));
        await WasRung();
        await _queue.Subscribe(_observerKey, EventTypeSet.From([_eventType]));
    }

    async Task Because() => await Deliver(EventAt(11));

    [Fact] void should_deliver_the_event_live() => _calls.ShouldContain($"handle:{_observerKey.ObserverId}");
    [Fact] void should_have_rung_it_only_before_it_subscribed() => _calls.Count(_ => _ == $"ring:{_observerKey.ObserverId}").ShouldEqual(1);
}
