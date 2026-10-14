// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts.Auditing;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Identities;
using Cratis.Chronicle.Storage.EventSequences;
using Cratis.Monads;

namespace Cratis.Chronicle.EventSequences.for_EventSequence;

public class when_storage_renumbers_past_another_activations_event : given.an_event_sequence_with_repairable_indexes
{
    int _liveWrites;
    async Task Establish()
    {
        await Seed(0);
        await Seed(1);
        var calls = 0;
        _eventSequenceStorage.Append(Arg.Any<EventSequenceNumber>(), Arg.Any<EventSourceType>(), Arg.Any<EventSourceId>(), Arg.Any<EventStreamType>(), Arg.Any<EventStreamId>(), Arg.Any<EventType>(), Arg.Any<CorrelationId>(), Arg.Any<IEnumerable<Causation>>(), Arg.Any<IEnumerable<IdentityId>>(), Arg.Any<IEnumerable<Tag>>(), Arg.Any<DateTimeOffset>(), Arg.Any<IDictionary<EventTypeGeneration, ExpandoObject>>(), Arg.Any<IDictionary<EventTypeGeneration, EventHash>>(), Arg.Any<Subject?>())
            .Returns(_ => Task.FromResult<Result<AppendedEvent, DuplicateEventSequenceNumber>>(calls++ == 0 ? new DuplicateEventSequenceNumber(1) : _log.Events[1]));
        await AppendAnEvent();
        _liveWrites = _claimWrites;
        _eventSourceId = EventSourceId.New();
    }
    async Task Because() => _result = await AppendAnEvent();

    [Fact] void should_defer_live_updates_past_the_gap() => _liveWrites.ShouldEqual(0);
    [Fact] void should_fold_the_gap_before_validating_the_next_append() => _rangeReads.ShouldEqual(1);
    [Fact] void should_refuse_the_duplicate_of_the_repaired_claim() => _result.ConstraintViolations.ShouldNotBeEmpty();
}
