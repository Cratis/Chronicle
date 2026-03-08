// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences.Concurrency;

namespace Cratis.Chronicle.EventSequences.Concurrency.for_ConcurrencyValidator.when_validating_multiple_scopes;

public class and_one_violation_occurs : given.a_concurrency_validator
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
            Arg.Any<EventStreamType>()).Returns(45UL); // Violation

        _eventSequenceStorage.GetTailSequenceNumber(
            Arg.Any<IEnumerable<EventType>>(),
            _eventSourceId2,
            Arg.Any<EventSourceType>(),
            Arg.Any<EventStreamId>(),
            Arg.Any<EventStreamType>()).Returns(41UL); // No violation
    }

    async Task Because() => _result = await _validator.Validate(_scopes);

    [Fact] void should_return_one_violation() => _result.Count().ShouldEqual(1);
    [Fact] void should_have_violation_for_first_event_source() => _result.Any(_ => _.EventSourceId == _eventSourceId1).ShouldBeTrue();
    [Fact] void should_not_have_violation_for_second_event_source() => _result.Any(_ => _.EventSourceId == _eventSourceId2).ShouldBeFalse();

    [Fact]
    void should_have_correct_violation_details()
    {
        var violation = _result.First(_ => _.EventSourceId == _eventSourceId1);
        violation.ExpectedSequenceNumber.Value.ShouldEqual(42UL);
        violation.ActualSequenceNumber.Value.ShouldEqual(45UL);
    }
}
