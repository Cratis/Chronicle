// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Alerts;
using Cratis.Chronicle.Observation.Alerts;

namespace Cratis.Chronicle.Observation.for_Observer.when_reporting_alert_state;

public class and_an_old_acknowledgment_arrives_after_a_new_change : given.an_observer
{
    long _oldRevision;
    bool _applied;

    async Task Establish()
    {
        await _observer.PartitionFailed("partition", 12UL, ["Failed"], "Stack");
        _oldRevision = _stateStorage.State.AlertRevision;
        await _observer.FailedPartitionRecovered("partition", 12UL);
        _observerAlerts.Reconcile(Arg.Any<ObserverAlertSnapshot>()).Returns(call => new ObserverAlertReceipt(call.Arg<ObserverAlertSnapshot>().LifecycleId, _oldRevision, ObserverAlertReconciliation.Applied));
        _applied = await ReportAlerts();
        ApplyAlertReports();
        _observerAlerts.ClearReceivedCalls();
    }

    async Task Because() => await _observer.RunWatchdogAsync();

    [Fact] void should_reject_the_mismatched_receipt() => _applied.ShouldBeFalse();
    [Fact] async Task should_keep_the_newer_ending_pending() => await _observerAlerts.Received(1).Reconcile(Arg.Is<ObserverAlertSnapshot>(_ => _.Revision > _oldRevision && _.Endings.Count == 1));
}
