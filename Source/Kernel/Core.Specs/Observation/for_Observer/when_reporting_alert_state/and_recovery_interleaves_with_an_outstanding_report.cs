// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Alerts;
using Cratis.Chronicle.Concepts.Alerts;
using Cratis.Chronicle.Observation.Alerts;

namespace Cratis.Chronicle.Observation.for_Observer.when_reporting_alert_state;

public class and_recovery_interleaves_with_an_outstanding_report : given.an_observer
{
    readonly TaskCompletionSource<ObserverAlertReceipt> _response = new();
    readonly TaskCompletionSource<ObserverAlertSnapshot> _started = new();
    ObserverAlertSnapshot _original;
    bool _oldApplied;

    async Task Establish()
    {
        await _observer.PartitionFailed("partition", 12UL, ["Failed"], "Stack");
        _observerAlerts.Reconcile(Arg.Any<ObserverAlertSnapshot>()).Returns(call =>
        {
            _started.TrySetResult(call.Arg<ObserverAlertSnapshot>());
            return _response.Task;
        });
    }

    async Task Because()
    {
        var report = ReportAlerts();
        _original = await _started.Task.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
        await _observer.FailedPartitionRecovered("partition", 12UL);
        _response.SetResult(new(_original.LifecycleId, _original.Revision, ObserverAlertReconciliation.Applied));
        _oldApplied = await report;
        ApplyAlertReports();
        await _observer.RunWatchdogAsync();
    }

    [Fact] void should_not_acknowledge_the_newer_level_from_an_old_receipt() => _oldApplied.ShouldBeFalse();
    [Fact] async Task should_report_the_committed_recovery_with_its_episode_reason() => await _observerAlerts.Received(1).Reconcile(Arg.Is<ObserverAlertSnapshot>(_ => _.Revision > _original.Revision && _.FailedPartitions.Count == 0 && _.Endings[_original.FailedPartitions.Single().Id.Value] == AlertClearedReason.Recovered));
}
