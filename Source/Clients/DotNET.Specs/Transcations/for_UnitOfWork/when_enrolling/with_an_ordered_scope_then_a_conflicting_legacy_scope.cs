// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Auditing;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSequences.Concurrency;

namespace Cratis.Chronicle.Transactions.for_UnitOfWork.when_enrolling;

public class with_an_ordered_scope_then_a_conflicting_legacy_scope : given.a_unit_of_work
{
    EventSourceId _eventSourceId;
    ConcurrencyScope _exactScope;
    BatchEvent _batchEvent;
    Exception _error;

    void Establish()
    {
        _eventSourceId = EventSourceId.New();
        _exactScope = new(42UL, _eventSourceId);
        _batchEvent = new();
        _unitOfWork.AddEvents(EventSequenceId.Log, [new(_eventSourceId, _batchEvent)], [new(_eventSourceId, _exactScope)]);
        _error = Catch.Exception(() => _unitOfWork.AddEvent(
            EventSequenceId.Log,
            _eventSourceId,
            new RejectedLegacyEvent(),
            Causation.Unknown(),
            concurrencyScope: new ConcurrencyScope(43UL, _eventSourceId)));
    }

    async Task Because() => await _unitOfWork.Commit();

    [Fact] void should_reject_the_conflicting_legacy_scope() => _error.ShouldBeOfExactType<ConflictingConcurrencyScopesForLabel>();
    [Fact] void should_not_stage_the_legacy_event() => _eventsAppended.Select(_ => _.Event).ShouldContainOnly(_batchEvent);
    [Fact] void should_keep_the_ordered_scope() => _concurrencyScopesAppended[_eventSourceId].ShouldEqual(_exactScope);

    record BatchEvent;
    record RejectedLegacyEvent;
}
