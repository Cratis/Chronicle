// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.EventSequences.for_AppendedEventsQueue.when_enqueuing;

public class with_no_subscribed_event_type_in_the_batch : given.a_single_subscriber_with_an_event_type
{
    async Task Because()
    {
        await _queue.Enqueue([AppendedEvent.EmptyWithEventType(new EventType("Other", EventTypeGeneration.First))]);
        await _queue.AwaitQueueDepletion();
    }

    [Fact] void should_not_deliver_any_events() => _handledEventsPerPartition.ShouldBeEmpty();
}
