// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSequences.Concurrency;

namespace Cratis.Chronicle.Transactions.for_UnitOfWork.when_enrolling;

public class with_a_scope_narrowed_to_an_event_source_different_from_its_label : given.a_unit_of_work
{
    FirstEvent _firstEvent;
    Exception _error;

    void Establish()
    {
        _firstEvent = new();
        _unitOfWork.AddEvents(EventSequenceId.Log, [new(EventSourceId.New(), _firstEvent)], []);
        _error = Catch.Exception(() => _unitOfWork.AddEvents(
            EventSequenceId.Log,
            [new(EventSourceId.New(), new RejectedEvent())],
            [new(EventSourceId.New(), new ConcurrencyScope(42UL, EventSourceId.New()))]));
    }

    async Task Because() => await _unitOfWork.Commit();

    [Fact] void should_fail_with_the_domain_exception() => _error.ShouldBeOfExactType<ConcurrencyScopeEventSourceIdDoesNotMatchLabel>();
    [Fact] void should_not_stage_the_rejected_event() => _eventsAppended.Select(_ => _.Event).ShouldContainOnly(_firstEvent);
    [Fact] void should_not_enroll_the_invalid_scope() => _concurrencyScopesAppended.ShouldBeEmpty();

    record FirstEvent;
    record RejectedEvent;
}
