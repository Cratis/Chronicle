// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences.Concurrency;
using Cratis.Monads;

namespace Cratis.Chronicle.EventSequences.Concurrency.for_ConcurrencyValidator.when_validating_single_scope;

/// <summary>
/// An ordinary scope naming a sequence number it expects still behaves exactly as it did before the before-first
/// expectation existed. An unreadable tail is not something it can compare against, and it never was - only a scope
/// that says <see cref="EventSequenceNumber.BeforeFirst"/> reads an empty tail as the expectation being met, and
/// only a scope that says it reads a non-empty one as a violation.
/// </summary>
public class and_scope_expects_an_actual_sequence_number_but_nothing_matches_the_narrowing : given.a_concurrency_validator
{
    EventSourceId _eventSourceId;
    ConcurrencyScope _scope;
    Option<ConcurrencyViolation> _result;

    void Establish()
    {
        _eventSourceId = EventSourceId.New();
        _scope = new ConcurrencyScope(42UL, true, null, null, new EventSourceType("Customer"), null);

        _eventSequenceStorage.GetTailSequenceNumber(
            _scope.EventTypes,
            _eventSourceId,
            _scope.EventSourceType,
            _scope.EventStreamId,
            _scope.EventStreamType).Returns(EventSequenceNumber.Unavailable);
    }

    async Task Because() => _result = await _validator.Validate(_eventSourceId, _scope);

    [Fact] void should_not_report_a_violation() => _result.HasValue.ShouldBeFalse();
    [Fact] void should_not_be_read_as_expecting_no_matching_event() => _scope.ExpectsNoMatchingEvent.ShouldBeFalse();
}
