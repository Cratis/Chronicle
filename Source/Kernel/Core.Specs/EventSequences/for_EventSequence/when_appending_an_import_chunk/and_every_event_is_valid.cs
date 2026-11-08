// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Imports;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_appending_an_import_chunk;

public class and_every_event_is_valid : given.an_event_sequence_with_imports
{
    async Task Because() => _chunkResult = await AppendChunk();

    [Fact] void should_settle() => _chunkResult.IsSettled.ShouldBeTrue();
    [Fact] void should_append_every_event() => _chunkResult.Outcomes.ShouldEachConformTo(_ => _.Kind == ImportEventOutcomeKind.Appended);
    [Fact] void should_store_the_chunk_in_one_write() => _writes.ShouldEqual(1);
    [Fact] void should_write_every_event() => _written.Count.ShouldEqual(3);
    [Fact] void should_keep_the_import_causation() => _written.ShouldEachConformTo(_ => _.Causation.First().Type == Concepts.Auditing.CausationType.Import);
    [Fact] async Task should_hold_back_observers_of_the_event_types() => await _appendedEventsQueues.Received(1).HoldBack(Arg.Any<string>(), Arg.Is<IEnumerable<EventTypeId>>(_ => _.Contains(_eventType.Id)));
    [Fact] async Task should_not_hand_the_events_to_live_delivery() => await _appendedEventsQueues.DidNotReceive().Enqueue(Arg.Any<IEnumerable<AppendedEvent>>());
}
