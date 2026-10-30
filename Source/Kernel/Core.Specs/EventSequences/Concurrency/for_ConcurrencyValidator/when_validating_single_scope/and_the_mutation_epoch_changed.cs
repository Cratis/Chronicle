// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.EventSequences.Concurrency;
using Cratis.Monads;

namespace Cratis.Chronicle.EventSequences.Concurrency.for_ConcurrencyValidator.when_validating_single_scope;

public class and_the_mutation_epoch_changed : given.a_concurrency_validator
{
    EventSourceId _eventSourceId;
    ConcurrencyScope _scope;
    Option<ConcurrencyViolation> _result;

    void Establish()
    {
        _eventSourceId = EventSourceId.New();
        _scope = new ConcurrencyScope(10, true, null, null, null, null) { MutationEpoch = new EventSequenceMutationEpoch(2) };
        _eventSequenceStorage.GetTailSequenceNumber(Arg.Any<IEnumerable<EventType>>(), _eventSourceId, Arg.Any<EventSourceType>(), Arg.Any<EventStreamId>(), Arg.Any<EventStreamType>()).Returns(10UL);
    }

    async Task Because() => _result = await _validator.Validate(_eventSourceId, _scope);

    [Fact] void should_report_a_violation() => _result.HasValue.ShouldBeTrue();
    [Fact] void should_report_the_event_source() => _result.AsT0.EventSourceId.ShouldEqual(_eventSourceId);
    [Fact] void should_not_name_a_conflicting_event() => _result.AsT0.ConflictingEventTypeId.ShouldBeNull();
}
