// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences.Concurrency;
using Cratis.Monads;

namespace Cratis.Chronicle.Grains.EventSequences.Concurrency.for_ConcurrencyValidator.when_validating_single_scope;

public class and_no_concurrency_violation_detected : given.a_concurrency_validator
{
    EventSourceId _eventSourceId;
    ConcurrencyScope _scope;
    Option<ConcurrencyViolation> _result;

    void Establish()
    {
        _eventSourceId = EventSourceId.New();
        _scope = new ConcurrencyScope(42, true, null, null, null, null);

        _eventSequenceStorage.GetTailSequenceNumber(
            _scope.EventTypes,
            _eventSourceId,
            _scope.EventSourceType,
            _scope.EventStreamId,
            _scope.EventStreamType).Returns(40UL);
    }

    async Task Because() => _result = await _validator.Validate(_eventSourceId, _scope);

    [Fact] void should_return_none() => _result.HasValue.ShouldBeFalse();
    [Fact] void should_call_storage_with_correct_parameters() => _eventSequenceStorage.Received(1).GetTailSequenceNumber(_scope.EventTypes, _eventSourceId, _scope.EventSourceType, _scope.EventStreamId, _scope.EventStreamType);
}
