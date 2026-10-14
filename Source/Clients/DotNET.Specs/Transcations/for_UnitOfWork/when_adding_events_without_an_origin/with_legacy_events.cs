// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Auditing;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSequences.Concurrency;

namespace Cratis.Chronicle.Transactions.for_UnitOfWork.when_adding_events_without_an_origin;

public class with_legacy_events : given.a_unit_recording_read_protection
{
    readonly ConcurrencyScope _scope = new(7, EventStreamType: "items");
    readonly object _event = new();
    void Establish() => _unitOfWork.AddEvent(EventSequenceId.Log, "source", _event, Causation.Unknown(), concurrencyScope: _scope);
    Task Because() => _unitOfWork.Commit();
    [Fact] void should_keep_the_scope_dictionary_identical() => _concurrencyScopesAppended.ShouldContainOnly(new KeyValuePair<EventSourceId, ConcurrencyScope>("source", _scope));
    [Fact] void should_keep_the_event() => _eventsAppended.Single().Event.ShouldBeSame(_event);
    [Fact] void should_not_call_the_read_protection_interface() => _readProtectionCalled.ShouldBeFalse();
}
