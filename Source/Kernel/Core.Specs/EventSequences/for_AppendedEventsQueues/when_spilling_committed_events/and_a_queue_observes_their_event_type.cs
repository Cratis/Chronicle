// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.EventSequences.for_AppendedEventsQueues.when_spilling_committed_events;

public class and_a_queue_observes_their_event_type : given.two_seeded_queues
{
    async Task Because() => await _queues.SpillMatchingToCatchup([EventOfType(subscribed_event_type.Id)]);

    [Fact] async Task should_hand_them_to_every_queue_that_observes_them() => await Task.WhenAll(_queueGrains.Select(_ => _.Received(1).SpillMatchingToCatchup(Arg.Any<IEnumerable<AppendedEvent>>())));
    [Fact] async Task should_not_deliver_them_live() => await Task.WhenAll(_queueGrains.Select(_ => _.DidNotReceive().Enqueue(Arg.Any<IEnumerable<AppendedEvent>>())));
}
