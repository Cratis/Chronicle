// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.EventSequences.for_AppendedEventsQueues.when_enqueuing;

/// <summary>
/// A redaction carries its own event type identifier, which is never part of any queue's subscribed-type union -
/// observers match a redaction through the original event type it stands in for. Routing a redaction through the
/// union would therefore narrow it away on every queue and silently stop all redaction propagation, so redactions
/// are force-broadcast to every queue and matched by the queues themselves.
/// </summary>
public class and_the_batch_is_a_redaction : given.two_seeded_queues
{
    AppendedEvent _redaction;

    void Establish() => _redaction = EventOfType(GlobalEventTypes.Redaction);

    async Task Because() => await _queues.Enqueue([_redaction]);

    [Fact] void should_deliver_the_redaction_to_every_queue()
    {
        foreach (var queueGrain in _queueGrains)
        {
            queueGrain.Received(1).Enqueue(Arg.Is<IEnumerable<AppendedEvent>>(events => events.Contains(_redaction)));
        }
    }
}
