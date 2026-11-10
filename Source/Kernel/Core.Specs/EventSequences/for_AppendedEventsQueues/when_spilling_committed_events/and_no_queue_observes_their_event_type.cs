// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.EventSequences.for_AppendedEventsQueues.when_spilling_committed_events;

public class and_no_queue_observes_their_event_type : given.two_seeded_queues
{
    async Task Because() => await _queues.SpillMatchingToCatchup([EventOfType(unsubscribed_event_type.Id)]);

    [Fact] async Task should_not_disturb_queues_whose_observers_do_not_want_them() => await Task.WhenAll(_queueGrains.Select(_ => _.DidNotReceive().SpillMatchingToCatchup(Arg.Any<IEnumerable<AppendedEvent>>())));
}
