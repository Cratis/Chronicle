// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences.Concurrency;

namespace Cratis.Chronicle.EventSequences.Concurrency.for_ConcurrencyValidator.when_validating_multiple_scopes;

public class with_mixed_scopes_some_should_not_be_validated : given.a_concurrency_validator
{
    ConcurrencyScopes _scopes;
    IEnumerable<ConcurrencyViolation> _result;
    EventSourceId _eventSourceId1;
    EventSourceId _eventSourceId2;
    EventSourceId _eventSourceId3;

    void Establish()
    {
        _eventSourceId1 = EventSourceId.New();
        _eventSourceId2 = EventSourceId.New();
        _eventSourceId3 = EventSourceId.New();

        _scopes = new ConcurrencyScopes(new Dictionary<EventSourceId, ConcurrencyScope>
        {
            { _eventSourceId1, ConcurrencyScope.None }, // Should not be validated
            { _eventSourceId2, new ConcurrencyScope(43, true, null, null, null, null) }, // Should be validated
            { _eventSourceId3, ConcurrencyScope.NotSet } // Should not be validated
        });

        _eventSequenceStorage.GetTailSequenceNumber(
            Arg.Any<IEnumerable<EventType>>(),
            _eventSourceId2,
            Arg.Any<EventSourceType>(),
            Arg.Any<EventStreamId>(),
            Arg.Any<EventStreamType>()).Returns(41UL); // No violation
    }

    async Task Because() => _result = await _validator.Validate(_scopes);

    [Fact] void should_return_no_violations() => _result.ShouldBeEmpty();
    [Fact] void should_only_call_storage_for_scope_that_can_be_validated() => _eventSequenceStorage.Received(1).GetTailSequenceNumber(Arg.Any<IEnumerable<EventType>>(), _eventSourceId2, Arg.Any<EventSourceType>(), Arg.Any<EventStreamId>(), Arg.Any<EventStreamType>());
    [Fact]
    void should_not_call_storage_for_scopes_that_can_not_be_validated()
    {
        _eventSequenceStorage.DidNotReceive().GetTailSequenceNumber(Arg.Any<IEnumerable<EventType>>(), _eventSourceId1, Arg.Any<EventSourceType>(), Arg.Any<EventStreamId>(), Arg.Any<EventStreamType>());
        _eventSequenceStorage.DidNotReceive().GetTailSequenceNumber(Arg.Any<IEnumerable<EventType>>(), _eventSourceId3, Arg.Any<EventSourceType>(), Arg.Any<EventStreamId>(), Arg.Any<EventStreamType>());
    }
}
