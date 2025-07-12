// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences.Concurrency;

namespace Cratis.Chronicle.Grains.EventSequences.Concurrency.for_ConcurrencyValidator.when_validating_multiple_scopes;

public class and_no_violations_occur : given.a_concurrency_validator
{
    ConcurrencyScopes _scopes;
    IEnumerable<ConcurrencyViolation> _result;
    EventSourceId _eventSourceId1;
    EventSourceId _eventSourceId2;

    void Establish()
    {
        _eventSourceId1 = EventSourceId.New();
        _eventSourceId2 = EventSourceId.New();

        _scopes = new ConcurrencyScopes(new Dictionary<EventSourceId, ConcurrencyScope>
        {
            { _eventSourceId1, new ConcurrencyScope(42, true, null, null, null, null) },
            { _eventSourceId2, new ConcurrencyScope(43, true, null, null, null, null) }
        });

        _eventSequenceStorage.GetTailSequenceNumber(
            Arg.Any<IEnumerable<EventType>>(),
            _eventSourceId1,
            Arg.Any<EventSourceType>(),
            Arg.Any<EventStreamId>(),
            Arg.Any<EventStreamType>()).Returns(40UL);

        _eventSequenceStorage.GetTailSequenceNumber(
            Arg.Any<IEnumerable<EventType>>(),
            _eventSourceId2,
            Arg.Any<EventSourceType>(),
            Arg.Any<EventStreamId>(),
            Arg.Any<EventStreamType>()).Returns(41UL);
    }

    async Task Because() => _result = await _validator.Validate(_scopes);

    [Fact] void should_return_no_violations() => _result.ShouldBeEmpty();
    [Fact] void should_call_storage_for_first_event_source() => _eventSequenceStorage.Received(1).GetTailSequenceNumber(Arg.Any<IEnumerable<EventType>>(), _eventSourceId1, Arg.Any<EventSourceType>(), Arg.Any<EventStreamId>(), Arg.Any<EventStreamType>());
    [Fact] void should_call_storage_for_second_event_source() => _eventSequenceStorage.Received(1).GetTailSequenceNumber(Arg.Any<IEnumerable<EventType>>(), _eventSourceId2, Arg.Any<EventSourceType>(), Arg.Any<EventStreamId>(), Arg.Any<EventStreamType>());
}
