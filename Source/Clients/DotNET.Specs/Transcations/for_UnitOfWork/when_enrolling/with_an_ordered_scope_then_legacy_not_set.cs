// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Auditing;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSequences.Concurrency;

namespace Cratis.Chronicle.Transactions.for_UnitOfWork.when_enrolling;

public class with_an_ordered_scope_then_legacy_not_set : given.a_unit_of_work
{
    EventSourceId _eventSourceId;
    ConcurrencyScope _exactScope;

    void Establish()
    {
        _eventSourceId = EventSourceId.New();
        _exactScope = new(42UL, _eventSourceId);
        _unitOfWork.AddEvents(
            EventSequenceId.Log,
            [new(_eventSourceId, new BatchEvent())],
            [new(_eventSourceId, _exactScope)]);
        _unitOfWork.AddEvent(EventSequenceId.Log, _eventSourceId, new LegacyEvent(), Causation.Unknown());
    }

    async Task Because() => await _unitOfWork.Commit();

    [Fact] void should_not_weaken_the_ordered_scope() => _concurrencyScopesAppended[_eventSourceId].ShouldEqual(_exactScope);
    [Fact] void should_stage_both_events() => _eventsAppended.Count().ShouldEqual(2);

    record BatchEvent;
    record LegacyEvent;
}
