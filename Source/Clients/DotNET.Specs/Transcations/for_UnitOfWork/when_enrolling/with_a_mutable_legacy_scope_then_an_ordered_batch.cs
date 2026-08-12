// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Auditing;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSequences.Concurrency;

namespace Cratis.Chronicle.Transactions.for_UnitOfWork.when_enrolling;

public class with_a_mutable_legacy_scope_then_an_ordered_batch : given.a_unit_of_work
{
    EventSourceId _legacyTarget;
    EventType _eventType;
    List<EventType> _eventTypes;

    void Establish()
    {
        _legacyTarget = EventSourceId.New();
        _eventType = new EventType("legacy", 1);
        _eventTypes = [_eventType];
        _unitOfWork.AddEvent(
            EventSequenceId.Log,
            _legacyTarget,
            new LegacyEvent(),
            Causation.Unknown(),
            concurrencyScope: new ConcurrencyScope(42UL, EventTypes: _eventTypes));
        _unitOfWork.AddEvents(
            EventSequenceId.Log,
            [new(EventSourceId.New(), new OrderedEvent())],
            []);
        _eventTypes.Clear();
    }

    async Task Because() => await _unitOfWork.Commit();

    [Fact] void should_commit_the_legacy_scope_with_its_snapshotted_event_type_filter() => _concurrencyScopesAppended[_legacyTarget].EventTypes.ShouldContainOnly(_eventType);

    record LegacyEvent;
    record OrderedEvent;
}
