// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Auditing;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSequences.Concurrency;

namespace Cratis.Chronicle.Transactions.for_UnitOfWork.when_enrolling;

public class with_a_legacy_scope_narrowed_to_another_event_source : given.a_unit_of_work
{
    EventSourceId _eventSourceId;
    ConcurrencyScope _scope;
    LegacyEvent _event;

    void Establish()
    {
        _eventSourceId = EventSourceId.New();
        _scope = new(42UL, EventSourceId.New());
        _event = new();
        _unitOfWork.AddEvent(
            EventSequenceId.Log,
            _eventSourceId,
            _event,
            Causation.Unknown(),
            concurrencyScope: _scope);
    }

    async Task Because() => await _unitOfWork.Commit();

    [Fact] void should_preserve_the_legacy_event() => _eventsAppended.Select(_ => _.Event).ShouldContainOnly(_event);
    [Fact] void should_preserve_the_legacy_scope_shape() => _concurrencyScopesAppended[_eventSourceId].ShouldEqual(_scope);

    record LegacyEvent;
}
