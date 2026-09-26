// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences.Concurrency;
using Cratis.Chronicle.Concepts.Identities;
using Cratis.Chronicle.Storage.EventSequences;
using Cratis.Monads;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_appending_many;

public class and_no_events_are_appended : given.appending_many_events
{
    readonly EventSourceId _readSource = "read-source";
    AppendManyResult _result;

    void Establish()
    {
        _events.Clear();
        _eventSequenceStorage.GetTailSequenceNumber(Arg.Any<IEnumerable<EventType>>(), _readSource, null, null, null)
            .Returns((EventSequenceNumber)4);
        _eventSequenceStorage.AppendMany(Arg.Any<IEnumerable<EventToAppendToStorage>>())
            .Returns(call => Task.FromResult(Result<IEnumerable<AppendedEvent>, DuplicateEventSequenceNumber>.Success(
                AppendedEventsFrom(call.Arg<IEnumerable<EventToAppendToStorage>>()))));
    }

    async Task Because() => _result = await _eventSequence.AppendMany(
        _events,
        CorrelationId.New(),
        [],
        Identity.System,
        new ConcurrencyScopes(new Dictionary<EventSourceId, ConcurrencyScope>
        {
            [_readSource] = new(4, true, null, null, null, [_eventType])
        }));

    [Fact] void should_validate_and_succeed() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_not_advance_the_next_sequence_number() => _stateStorage.State.SequenceNumber.ShouldEqual(EventSequenceNumber.First);
    [Fact] void should_not_enqueue_an_empty_batch() => _appendedEventsQueues.DidNotReceiveWithAnyArgs().Enqueue(Arg.Any<IEnumerable<AppendedEvent>>());
    [Fact] void should_not_update_constraint_indexes() => _constraintIndexSequenceNumbers.ShouldBeEmpty();
    [Fact] void should_not_append_any_event() => _eventSequenceStorage.DidNotReceive().AppendMany(Arg.Is<IEnumerable<EventToAppendToStorage>>(_ => _.Any()));
    [Fact] void should_filter_the_validation_by_source_and_type() =>
        _eventSequenceStorage.Received().GetTailSequenceNumber(Arg.Any<IEnumerable<EventType>>(), _readSource, null, null, null);
}
