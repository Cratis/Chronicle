// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Alerts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.Alerts.for_ObserverAlerts.when_reconciling;

public class and_durable_history_has_advanced_since_the_last_call : given.an_alert_tracker
{
    async Task Establish()
    {
        await _tracker.Reconcile(_snapshot);
        _snapshot = _snapshot with { FailedPartitions = [_snapshot.FailedPartitions.Single() with { Id = FailedPartitionId.New() }], Revision = 2 };
        RecordDurable(RaisedForSnapshot());
        _appends.Clear();
    }

    async Task Because() => _receipt = await _tracker.Reconcile(_snapshot);

    [Fact] void should_read_from_the_previous_durable_tail() => _sequenceStorage.ReceivedCalls().Any(call => call.GetArguments()[0] is EventSequenceNumber number && number == (EventSequenceNumber)1UL).ShouldBeTrue();
    [Fact] void should_clear_only_the_absent_episode() => _appends.Single().ShouldBeOfExactType<AlertCleared>();
    [Fact] void should_not_repeat_the_external_raise() => _appends.OfType<AlertRaised>().ShouldBeEmpty();
    [Fact] void should_acknowledge_application() => _receipt.Outcome.ShouldEqual(ObserverAlertReconciliation.Applied);
}
