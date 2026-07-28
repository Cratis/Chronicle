// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.EventSequences.for_AppendedEventsQueues.when_enqueuing;

/// <summary>
/// Establishes that the seeded router really does narrow delivery, which is what makes the redaction
/// force-broadcast load-bearing rather than incidental.
/// </summary>
public class and_the_batch_has_no_subscribed_event_types : given.two_seeded_queues
{
    AppendedEvent _appendedEvent;

    void Establish() => _appendedEvent = EventOfType(unsubscribed_event_type.Id);

    async Task Because() => await _queues.Enqueue([_appendedEvent]);

    [Fact] void should_not_deliver_to_any_queue()
    {
        foreach (var queueGrain in _queueGrains)
        {
            queueGrain.DidNotReceive().Enqueue(Arg.Any<IEnumerable<AppendedEvent>>());
        }
    }
}
