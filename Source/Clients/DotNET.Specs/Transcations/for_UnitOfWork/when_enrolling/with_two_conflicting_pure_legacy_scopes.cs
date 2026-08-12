// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Auditing;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSequences.Concurrency;

namespace Cratis.Chronicle.Transactions.for_UnitOfWork.when_enrolling;

public class with_two_conflicting_pure_legacy_scopes : given.a_unit_of_work
{
    EventSourceId _eventSourceId;
    ConcurrencyScope _lastScope;

    void Establish()
    {
        _eventSourceId = EventSourceId.New();
        _lastScope = new(43UL, _eventSourceId);
        _unitOfWork.AddEvent(EventSequenceId.Log, _eventSourceId, new FirstEvent(), Causation.Unknown(), concurrencyScope: new ConcurrencyScope(42UL, _eventSourceId));
        _unitOfWork.AddEvent(EventSequenceId.Log, _eventSourceId, new SecondEvent(), Causation.Unknown(), concurrencyScope: _lastScope);
    }

    async Task Because() => await _unitOfWork.Commit();

    [Fact] void should_preserve_legacy_last_explicit_scope_semantics() => _concurrencyScopesAppended[_eventSourceId].ShouldEqual(_lastScope);
    [Fact] void should_stage_both_legacy_events() => _eventsAppended.Count().ShouldEqual(2);

    record FirstEvent;
    record SecondEvent;
}
