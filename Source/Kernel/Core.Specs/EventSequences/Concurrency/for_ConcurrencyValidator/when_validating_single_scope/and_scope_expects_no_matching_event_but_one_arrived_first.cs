// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences.Concurrency;
using Cratis.Monads;

namespace Cratis.Chronicle.EventSequences.Concurrency.for_ConcurrencyValidator.when_validating_single_scope;

/// <summary>
/// The race this whole change exists for. Nothing matched the narrowing when the scope was resolved, another writer
/// opened the same narrowed partition in between, and this append would previously have landed unchecked next to it -
/// two writers each believing they created the first event in the scope. The tail read now answers with an actual
/// number where the scope expects none, which is a violation.
/// </summary>
public class and_scope_expects_no_matching_event_but_one_arrived_first : given.a_concurrency_validator
{
    EventSourceId _eventSourceId;
    ConcurrencyScope _scope;
    Option<ConcurrencyViolation> _result;

    void Establish()
    {
        _eventSourceId = EventSourceId.New();
        _scope = new ConcurrencyScope(EventSequenceNumber.BeforeFirst, true, null, null, new EventSourceType("Customer"), null);

        _eventSequenceStorage.GetTailSequenceNumber(
            _scope.EventTypes,
            _eventSourceId,
            _scope.EventSourceType,
            _scope.EventStreamId,
            _scope.EventStreamType).Returns(7UL);
    }

    async Task Because() => _result = await _validator.Validate(_eventSourceId, _scope);

    [Fact] void should_report_a_violation() => _result.HasValue.ShouldBeTrue();
    [Fact] void should_report_the_event_source_it_was_validating() => _result.AsT0.EventSourceId.ShouldEqual(_eventSourceId);
    [Fact] void should_report_that_it_expected_no_event_to_exist() => _result.AsT0.ExpectedSequenceNumber.ShouldEqual(EventSequenceNumber.BeforeFirst);
    [Fact] void should_report_the_sequence_number_of_what_is_there_instead() => _result.AsT0.ActualSequenceNumber.Value.ShouldEqual(7UL);
}
