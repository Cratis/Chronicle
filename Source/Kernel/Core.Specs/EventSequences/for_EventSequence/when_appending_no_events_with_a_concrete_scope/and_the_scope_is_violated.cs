// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences.Concurrency;
using Cratis.Chronicle.Concepts.Identities;
using Cratis.Chronicle.Storage.EventSequences;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_appending_no_events_with_a_concrete_scope;

public class and_the_scope_is_violated : given.an_event_sequence
{
    AppendManyResult _result;

    void Establish() => _eventSequenceStorage.GetTailSequenceNumber(null, _eventSourceId, null, null, null).Returns((EventSequenceNumber)43UL);

    async Task Because() => _result = await _eventSequence.AppendMany(
        [],
        CorrelationId.New(),
        [],
        Identity.System,
        new ConcurrencyScopes(new Dictionary<EventSourceId, ConcurrencyScope>
        {
            [_eventSourceId] = new(42UL, true, null, null, null, null)
        }));

    [Fact] void should_fail() => _result.IsSuccess.ShouldBeFalse();
    [Fact] void should_report_that_concurrency_was_checked() => _result.ConcurrencyCheckPerformed.ShouldBeTrue();
    [Fact] void should_validate_the_concrete_scope() => _eventSequenceStorage.Received(1).GetTailSequenceNumber(null, _eventSourceId, null, null, null);
    [Fact] void should_report_one_violation() => _result.ConcurrencyViolations.Count().ShouldEqual(1);
    [Fact] void should_identify_the_scope_label() => _result.ConcurrencyViolations.Single().EventSourceId.ShouldEqual(_eventSourceId);
    [Fact] void should_report_the_expected_position() => _result.ConcurrencyViolations.Single().ExpectedSequenceNumber.ShouldEqual((EventSequenceNumber)42UL);
    [Fact] void should_report_the_actual_position() => _result.ConcurrencyViolations.Single().ActualSequenceNumber.ShouldEqual((EventSequenceNumber)43UL);
    [Fact] void should_not_append_even_an_empty_batch() => _eventSequenceStorage.DidNotReceive().AppendMany(Arg.Any<IEnumerable<EventToAppendToStorage>>());
    [Fact] void should_not_advance_the_sequence_number() => _stateStorage.State.SequenceNumber.ShouldEqual(EventSequenceNumber.First);
    [Fact] void should_not_dispatch_any_events() => _appendedEventsQueues.DidNotReceiveWithAnyArgs().Enqueue(Arg.Any<IEnumerable<AppendedEvent>>());
    [Fact] void should_not_update_constraint_indexes() => _constraintIndexSequenceNumbers.ShouldBeEmpty();
}
