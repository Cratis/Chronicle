// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences.Concurrency;
using Cratis.Monads;

namespace Cratis.Chronicle.EventSequences.Concurrency.for_ConcurrencyValidator.when_validating_single_scope;

/// <summary>
/// The first append into a narrowed scope: the strategy found no event matching the narrowing, and none has appeared
/// since. The expectation holds, so the append proceeds. This is the arm that must keep working - making the first
/// append checked is worthless if it rejects the ordinary case of genuinely being first.
/// </summary>
public class and_scope_expects_no_matching_event_and_none_exists : given.a_concurrency_validator
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
            _scope.EventStreamType).Returns(EventSequenceNumber.Unavailable);
    }

    async Task Because() => _result = await _validator.Validate(_eventSourceId, _scope);

    [Fact] void should_not_report_a_violation() => _result.HasValue.ShouldBeFalse();
    [Fact] void should_read_the_tail_through_the_scopes_own_narrowing() =>
        _eventSequenceStorage.Received(1).GetTailSequenceNumber(_scope.EventTypes, _eventSourceId, _scope.EventSourceType, _scope.EventStreamId, _scope.EventStreamType);
}
