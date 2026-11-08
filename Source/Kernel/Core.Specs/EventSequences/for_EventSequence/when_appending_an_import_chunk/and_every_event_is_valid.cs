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
    [Fact] async Task should_hand_the_observers_of_the_chunk_to_catch_up() => await _appendedEventsQueues.Received(1).SpillMatchingToCatchup(Arg.Is<IEnumerable<AppendedEvent>>(_ => _.Count() == 3));
    [Fact] async Task should_not_search_the_log_for_an_interrupted_commit() => await _eventSequenceStorage.DidNotReceiveWithAnyArgs().GetRange(default!, default!, default, default, default, default);
    [Fact] async Task should_not_hand_the_events_to_live_delivery() => await _appendedEventsQueues.DidNotReceive().Enqueue(Arg.Any<IEnumerable<AppendedEvent>>());
}
