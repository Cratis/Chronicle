// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.EventSequences.for_AppendedEventsQueue.when_asking_whether_an_observer_is_dormant;

/// <summary>
/// An observer that unsubscribed is not dormant, even though the queue released it earlier: reporting it as dormant
/// would make inspection read it as subscribed.
/// </summary>
public class and_it_unsubscribed_after_being_released : given.a_queue_with_an_unsubscribed_instance
{
    bool _dormant;

    async Task Establish()
    {
        await _queue.Subscribe(_observerKey, EventTypeSet.From([_eventType]));
        await _queue.TryMarkDormant(_observerKey);
    }

    async Task Because()
    {
        await _queue.Unsubscribe(_observerKey);
        _dormant = await _queue.IsDormant(_observerKey);
    }

    [Fact] void should_not_report_it_as_dormant() => _dormant.ShouldBeFalse();
}
