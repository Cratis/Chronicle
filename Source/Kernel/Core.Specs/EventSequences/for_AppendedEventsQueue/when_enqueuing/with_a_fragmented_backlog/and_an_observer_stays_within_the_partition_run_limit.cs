// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;

namespace Cratis.Chronicle.EventSequences.for_AppendedEventsQueue.when_enqueuing.with_a_fragmented_backlog;

/// <summary>
/// A backlog that takes exactly the limit of sequential per-partition deliveries stays on live delivery, in sequence order.
/// </summary>
public class and_an_observer_stays_within_the_partition_run_limit : given.a_fragmented_backlog
{
    async Task Because() => await EnqueueAndDeplete(Alternating(Limit, _eventType));

    [Fact] void should_hand_every_event_to_the_observer_in_sequence_order() => HandledFirstSequenceNumbers(_observer).SequenceEqual(Enumerable.Range(0, Limit).Select(_ => (ulong)_)).ShouldBeTrue();
    [Fact] void should_deliver_one_run_per_event() => _observer.Received(Limit).Handle(Arg.Any<Key>(), Arg.Any<IEnumerable<AppendedEvent>>());
    [Fact] void should_not_spill_the_observer() => _observer.DidNotReceive().CatchUp();
    [Fact] async Task should_keep_its_subscription() => (await _queue.GetSubscriptions()).Select(_ => _.ObserverKey).ShouldContainOnly(_observerKey);
}
