// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences.Concurrency;
using Cratis.Monads;

namespace Cratis.Chronicle.Grains.EventSequences.Concurrency.for_ConcurrencyValidator.when_validating_single_scope;

public class with_complex_scope_parameters : given.a_concurrency_validator
{
    EventSourceId _eventSourceId;
    ConcurrencyScope _scope;
    Option<ConcurrencyViolation> _result;
    EventStreamType _eventStreamType;
    EventStreamId _eventStreamId;
    EventSourceType _eventSourceType;
    IEnumerable<EventType> _eventTypes;

    void Establish()
    {
        _eventSourceId = EventSourceId.New();
        _eventStreamType = "SomeStreamType";
        _eventStreamId = "SomeStreamId";
        _eventSourceType = "SomeSourceType";
        _eventTypes = [new EventType("SomeEventType", 1), new EventType("AnotherEventType", 1)];

        _scope = new ConcurrencyScope(42, true, _eventStreamType, _eventStreamId, _eventSourceType, _eventTypes);

        _eventSequenceStorage.GetTailSequenceNumber(
            _eventTypes,
            _eventSourceId,
            _eventSourceType,
            _eventStreamId,
            _eventStreamType).Returns(40UL);
    }

    async Task Because() => _result = await _validator.Validate(_eventSourceId, _scope);

    [Fact] void should_return_none() => _result.HasValue.ShouldBeFalse();
    [Fact] void should_call_storage_with_all_scope_parameters() => _eventSequenceStorage.Received(1).GetTailSequenceNumber(_eventTypes, _eventSourceId, _eventSourceType, _eventStreamId, _eventStreamType);
}
