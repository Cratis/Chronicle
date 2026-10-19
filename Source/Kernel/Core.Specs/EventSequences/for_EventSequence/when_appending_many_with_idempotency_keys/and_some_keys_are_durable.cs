// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_appending_many_with_idempotency_keys;

public class and_some_keys_are_durable : given.an_idempotent_batch
{
    void Establish() => DurableAt(1, 4);
    async Task Because() => _batchResult = await AppendBatch();
    [Fact] void should_succeed() => _batchResult.IsSuccess.ShouldBeTrue();
    [Fact] void should_merge_numbers_in_request_order() => _batchResult.Result.SequenceNumbers.ShouldEqual([5, 4, 6]);
    [Fact] void should_merge_outcomes_in_request_order() => _batchResult.Outcomes.ShouldEqual([AppendOutcome.Appended, AppendOutcome.AlreadyAppended, AppendOutcome.Appended]);
    [Fact] void should_only_store_new_events_in_order() => _written.Select(@event => @event.EventSourceId).ShouldEqual([_events[0].EventSourceId, _events[2].EventSourceId]);
    [Fact] void should_only_store_new_identities() => _written.Select(@event => @event.Identity!.Id).ShouldEqual(["append-key:one", "append-key:three"]);
    [Fact] async Task should_enqueue_only_new_events() => await _appendedEventsQueues.Received(1).Enqueue(Arg.Is<IEnumerable<AppendedEvent>>(events => events.Count() == 2));
}
