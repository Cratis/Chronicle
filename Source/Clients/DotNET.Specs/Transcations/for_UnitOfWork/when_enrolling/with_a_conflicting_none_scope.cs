// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSequences.Concurrency;

namespace Cratis.Chronicle.Transactions.for_UnitOfWork.when_enrolling;

public class with_a_conflicting_none_scope : given.a_unit_of_work
{
    EventSourceId _scopeLabel;
    ConcurrencyScope _originalScope;
    FirstEvent _firstEvent;
    Exception _error;

    void Establish()
    {
        _scopeLabel = EventSourceId.New();
        _originalScope = new(42UL, EventTypes: [new EventType("event", 1)]);
        _firstEvent = new();
        _unitOfWork.AddEvents(EventSequenceId.Log, [new(EventSourceId.New(), _firstEvent)], [new(_scopeLabel, _originalScope)]);
        _error = Catch.Exception(() => _unitOfWork.AddEvents(
            EventSequenceId.Log,
            [new(EventSourceId.New(), new RejectedEvent())],
            [new(_scopeLabel, ConcurrencyScope.None)]));
    }

    async Task Because() => await _unitOfWork.Commit();

    [Fact] void should_fail_with_the_domain_exception() => _error.ShouldBeOfExactType<ConflictingConcurrencyScopesForLabel>();
    [Fact] void should_not_stage_the_rejected_event() => _eventsAppended.Select(_ => _.Event).ShouldContainOnly(_firstEvent);
    [Fact] void should_keep_the_original_exact_revision() => _concurrencyScopesAppended[_scopeLabel].SequenceNumber.ShouldEqual(_originalScope.SequenceNumber);
    [Fact] void should_keep_the_original_event_type_filter() => _concurrencyScopesAppended[_scopeLabel].EventTypes.ShouldContainOnly(_originalScope.EventTypes);

    record FirstEvent;
    record RejectedEvent;
}
