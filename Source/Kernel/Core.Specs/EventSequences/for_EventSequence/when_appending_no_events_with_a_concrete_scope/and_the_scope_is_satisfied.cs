// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences.Concurrency;
using Cratis.Chronicle.Concepts.Identities;
using Cratis.Chronicle.Storage.EventSequences;
using Cratis.Monads;
using Orleans.TestKit;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_appending_no_events_with_a_concrete_scope;

public class and_the_scope_is_satisfied : given.an_event_sequence
{
    AppendManyResult _result;
    CorrelationId _correlationId;

    void Establish()
    {
        _silo.StorageStats<EventSequence, EventSequenceState>().ResetCounts();
        _correlationId = CorrelationId.New();
        _eventSequenceStorage.GetTailSequenceNumber(null, _eventSourceId, null, null, null).Returns((EventSequenceNumber)42UL);
        _eventSequenceStorage.AppendMany(Arg.Any<IEnumerable<EventToAppendToStorage>>())
            .Returns(Result<IEnumerable<AppendedEvent>, DuplicateEventSequenceNumber>.Success([]));
    }

    async Task Because() => _result = await _eventSequence.AppendMany(
        [],
        _correlationId,
        [],
        Identity.System,
        new ConcurrencyScopes(new Dictionary<EventSourceId, ConcurrencyScope>
        {
            [_eventSourceId] = new(42UL, true, null, null, null, null)
        }));

    [Fact] void should_succeed() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_report_that_concurrency_was_checked() => _result.ConcurrencyCheckPerformed.ShouldBeTrue();
    [Fact] void should_validate_the_concrete_scope() => _eventSequenceStorage.Received(1).GetTailSequenceNumber(null, _eventSourceId, null, null, null);
    [Fact] void should_keep_the_correlation_id() => _result.CorrelationId.ShouldEqual(_correlationId);
    [Fact] void should_return_no_sequence_numbers() => _result.SequenceNumbers.ShouldBeEmpty();
    [Fact] void should_send_an_empty_batch_to_storage() => _eventSequenceStorage.Received(1).AppendMany(Arg.Is<IEnumerable<EventToAppendToStorage>>(events => !events.Any()));
    [Fact] void should_not_advance_the_sequence_number() => _stateStorage.State.SequenceNumber.ShouldEqual(EventSequenceNumber.First);
    [Fact] void should_not_dispatch_any_events() => _appendedEventsQueues.DidNotReceiveWithAnyArgs().Enqueue(Arg.Any<IEnumerable<AppendedEvent>>());
    [Fact] void should_not_update_constraint_indexes() => _constraintIndexSequenceNumbers.ShouldBeEmpty();
    [Fact] void should_not_persist_sequence_state() => _silo.StorageStats<EventSequence, EventSequenceState>().Writes.ShouldEqual(0);
}
