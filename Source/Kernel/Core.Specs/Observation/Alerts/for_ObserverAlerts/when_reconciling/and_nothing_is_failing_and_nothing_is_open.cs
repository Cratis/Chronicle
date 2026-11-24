// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Alerts;
using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Observation.Alerts.for_ObserverAlerts.when_reconciling;

/// <summary>
/// A healthy report on an activation that already folded the history, with no incident open, has nothing to close: it
/// neither waits on the cluster-wide append barrier nor reads the System event log again (#6113).
/// </summary>
public class and_nothing_is_failing_and_nothing_is_open : given.an_alert_tracker
{
    ObserverAlertSnapshot _healthy;

    async Task Establish()
    {
        _healthy = _snapshot with { FailedPartitions = [] };
        GivenHistory();
        await _tracker.Reconcile(_healthy);
        _sequence.ClearReceivedCalls();
        _sequenceStorage.ClearReceivedCalls();
    }

    async Task Because() => _receipt = await _tracker.Reconcile(_healthy);

    [Fact] void should_apply_the_report() => _receipt.Outcome.ShouldEqual(ObserverAlertReconciliation.Applied);
    [Fact] async Task should_not_drain_appends() => await _sequence.DidNotReceive().DrainAppends();
    [Fact] async Task should_not_read_the_history() => await _sequenceStorage.DidNotReceive().GetFromSequenceNumber(Arg.Any<EventSequenceNumber>(), Arg.Any<EventSourceId>(), eventTypes: Arg.Any<IEnumerable<EventType>>());
}
