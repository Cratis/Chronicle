// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Auditing;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSequences.Concurrency;

namespace Cratis.Chronicle.Transactions.for_UnitOfWork.when_enrolling;

public class with_a_conflicting_exact_scope_across_legacy_and_batch_calls : given.a_unit_of_work
{
    EventSourceId _eventSourceId;
    ConcurrencyScope _originalScope;
    FirstEvent _firstEvent;
    Exception _error;

    void Establish()
    {
        _eventSourceId = EventSourceId.New();
        _originalScope = new(42UL, _eventSourceId);
        _firstEvent = new();
        _unitOfWork.AddEvent(EventSequenceId.Log, _eventSourceId, _firstEvent, Causation.Unknown(), concurrencyScope: _originalScope);
        _error = Catch.Exception(() => _unitOfWork.AddEvents(
            EventSequenceId.Log,
            [new(EventSourceId.New(), new RejectedEvent())],
            [new(_eventSourceId, new ConcurrencyScope(43UL, _eventSourceId))]));
    }

    async Task Because() => await _unitOfWork.Commit();

    [Fact] void should_fail_with_the_domain_exception() => _error.ShouldBeOfExactType<ConflictingConcurrencyScopesForLabel>();
    [Fact] void should_not_stage_the_rejected_event() => _eventsAppended.Select(_ => _.Event).ShouldContainOnly(_firstEvent);
    [Fact] void should_keep_the_original_exact_scope() => ReferenceEquals(_concurrencyScopesAppended[_eventSourceId], _originalScope).ShouldBeTrue();

    record FirstEvent;
    record RejectedEvent;
}
