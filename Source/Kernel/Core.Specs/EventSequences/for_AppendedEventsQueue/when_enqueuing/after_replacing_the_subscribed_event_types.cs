// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.EventSequences.for_AppendedEventsQueue.when_enqueuing;

public class after_replacing_the_subscribed_event_types : given.a_single_subscriber_with_an_event_type
{
    AppendedEvent _matching;
    AppendedEvent _previous;

    async Task Establish()
    {
        _previous = AppendedEvent.EmptyWithEventType(_eventType);
        var replacement = new EventType("Replacement", EventTypeGeneration.First);
        _matching = AppendedEvent.EmptyWithEventType(replacement);
        await _queue.Subscribe(_observerKey, [replacement]);
    }

    async Task Because()
    {
        await _queue.Enqueue([_previous, _matching]);
        await _queue.AwaitQueueDepletion();
    }

    [Fact] void should_deliver_only_the_replacement_type() => _handledEventsPerPartition.Values.SelectMany(partition => partition).SelectMany(batch => batch.Events).ShouldContainOnly(_matching);
    [Fact] void should_replace_rather_than_duplicate_the_observer() => _handledEventsPerPartition.Values.Sum(partition => partition.Count).ShouldEqual(1);
}
