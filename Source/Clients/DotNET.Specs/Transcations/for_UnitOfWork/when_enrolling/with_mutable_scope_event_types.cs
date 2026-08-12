// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSequences.Concurrency;

namespace Cratis.Chronicle.Transactions.for_UnitOfWork.when_enrolling;

public class with_mutable_scope_event_types : given.a_unit_of_work
{
    EventSourceId _scopeLabel;
    EventType _eventType;
    List<EventType> _eventTypes;

    void Establish()
    {
        _scopeLabel = EventSourceId.New();
        _eventType = new EventType("event", 1);
        _eventTypes = [_eventType];
        _unitOfWork.AddEvents(
            EventSequenceId.Log,
            [new(EventSourceId.New(), new StagedEvent())],
            [new(_scopeLabel, new ConcurrencyScope(42UL, EventTypes: _eventTypes))]);
        _eventTypes.Clear();
    }

    async Task Because() => await _unitOfWork.Commit();

    [Fact] void should_commit_the_snapshotted_event_type_filter() => _concurrencyScopesAppended[_scopeLabel].EventTypes.ShouldContainOnly(_eventType);

    record StagedEvent;
}
