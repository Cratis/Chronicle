// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Alerts;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.Alerts;

namespace Cratis.Chronicle.Observation.for_Observer.when_reporting_alert_state;

public class and_a_legacy_quarantine_identity_is_adopted : given.an_observer
{
    readonly Guid _adopted = Guid.NewGuid();
    Guid? _ordinaryReportIdentity;

    async Task Establish()
    {
        _stateStorage.State = _stateStorage.State with { RunningState = ObserverRunningState.Quarantined, QuarantineEpisodeId = null };
        await Crash();
        _observerAlerts.Reconcile(Arg.Any<ObserverAlertSnapshot>()).Returns(call =>
        {
            var snapshot = call.Arg<ObserverAlertSnapshot>();
            if (snapshot.QuarantineEpisodeId is null)
            {
                return new ObserverAlertReceipt(snapshot.LifecycleId, snapshot.Revision, ObserverAlertReconciliation.RetryRequired, _adopted);
            }

            _ordinaryReportIdentity = _stateStorage.State.QuarantineEpisodeId;
            return new ObserverAlertReceipt(snapshot.LifecycleId, snapshot.Revision, ObserverAlertReconciliation.Applied);
        });
    }

    async Task Because()
    {
        await ReportAlerts();
        await ReportAlerts();
        await Crash();
        await ReportAlerts();
    }

    [Fact] void should_persist_the_adopted_identity_before_ordinary_reporting() => _ordinaryReportIdentity.ShouldEqual(_adopted);
    [Fact] async Task should_ask_for_adoption_only_once_even_across_crash() => await _observerAlerts.Received(1).Reconcile(Arg.Is<ObserverAlertSnapshot>(_ => _.IsQuarantined && _.QuarantineEpisodeId == null));
    [Fact] void should_keep_the_adopted_identity() => _stateStorage.State.QuarantineEpisodeId.ShouldEqual(_adopted);
}
