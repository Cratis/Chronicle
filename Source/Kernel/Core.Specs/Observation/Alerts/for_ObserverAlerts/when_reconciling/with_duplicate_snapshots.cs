// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Alerts;
using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Observation.Alerts.for_ObserverAlerts.when_reconciling;

public class with_duplicate_snapshots : given.an_alert_tracker
{
    async Task Establish() => await _tracker.Reconcile(_snapshot);

    async Task Because() => await _tracker.Reconcile(_snapshot);

    [Fact] void should_raise_only_once() => _appends.OfType<AlertRaised>().Count().ShouldEqual(1);
    [Fact] void should_guard_the_first_append_against_existing_history() => _lastScope.SequenceNumber.ShouldEqual(EventSequenceNumber.BeforeFirst);
    [Fact] void should_scope_the_check_to_the_observer() => _lastScope.EventSourceId.ShouldBeTrue();
    [Fact] async Task should_read_only_this_observers_history() => await _sequenceStorage.Received(1).GetFromSequenceNumber(EventSequenceNumber.First, (EventSourceId)$"store/namespace/{_key}", eventTypes: Arg.Is<IEnumerable<EventType>>(types => types.Select(type => type.Id).ToHashSet().SetEquals(new[] { typeof(AlertRaised).GetEventType().Id, typeof(AlertEscalated).GetEventType().Id, typeof(AlertCleared).GetEventType().Id })));
}
