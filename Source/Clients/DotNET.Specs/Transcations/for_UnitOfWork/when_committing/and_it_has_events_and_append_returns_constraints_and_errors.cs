// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


using System.Collections.Immutable;
using Cratis.Chronicle.Auditing;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Events.Constraints;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Transactions.for_UnitOfWork.when_committing;

public class and_it_has_events_and_append_returns_constraints_and_errors : given.a_unit_of_work_and_events_and_constraint_violations_and_errors
{
    IEnumerable<EventForEventSourceId> _events;
    AppendManyResult _result;

    void Establish()
    {
        _eventSequence
            .When(_ => _.AppendMany(Arg.Any<IEnumerable<EventForEventSourceId>>()))
            .Do(callInfo => _events = callInfo.Arg<IEnumerable<EventForEventSourceId>>());

        _result = new AppendManyResult
        {
            CorrelationId = _correlationId,
            ConstraintViolations = new[]
            {
                new ConstraintViolation(EventType.Unknown, EventSequenceNumber.First, "some constraint", "some message", []),
                new ConstraintViolation(EventType.Unknown, 42UL, "some other constraint", "some message", [])
            }.ToImmutableList(),
            Errors = new[]
            {
                new AppendError("some error"),
                new AppendError("some other error")
            }.ToImmutableList()
        };

        _eventSequence
            .AppendMany(Arg.Any<IEnumerable<EventForEventSourceId>>())
            .Returns(_result);
    }

    async Task Because() => await _unitOfWork.Commit();

    [Fact] void should_append_events_to_event_sequence() => _events.ShouldContainOnly(new(_firstEventEventSourceId, _firstEvent, _firstEventCausation), new(_secondEventEventSourceId, _secondEvent, _secondEventCausation));
    [Fact] void should_have_events_in_unit_of_work() => _unitOfWork.GetEvents().ShouldContainOnly(_firstEvent, _secondEvent);
    [Fact] void should_call_on_completed() => _onCompletedCalled.ShouldBeTrue();
    [Fact] void should_have_constraint_violations() => _unitOfWork.GetConstraintViolations().ShouldContainOnly(_result.ConstraintViolations);
    [Fact] void should_have_errors() => _unitOfWork.GetAppendErrors().ShouldContainOnly(_result.Errors);
    [Fact] void should_not_be_successful() => _unitOfWork.IsSuccess.ShouldBeFalse();
}
