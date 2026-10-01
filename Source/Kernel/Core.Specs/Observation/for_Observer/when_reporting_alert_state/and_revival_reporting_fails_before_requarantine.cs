// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Alerts;
using Cratis.Chronicle.Concepts.Alerts;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.States;

namespace Cratis.Chronicle.Observation.for_Observer.when_reporting_alert_state;

public class and_revival_reporting_fails_before_requarantine : given.an_observer
{
    Guid _original;

    async Task Establish()
    {
        await _observer.TransitionTo<QuarantinedObserver>();
        _original = _stateStorage.State.QuarantineEpisodeId!.Value;
        await ReportAlerts();
        await _observer.Subscribe<ObserverSubscriber>(ObserverType.Reactor, [], SiloAddress.Zero);
        FailAlertReports(new TimeoutException());
        await ReportAlerts();
        ApplyAlertReports();
        _observerAlerts.ClearReceivedCalls();
    }

    async Task Because()
    {
        await _observer.TransitionTo<QuarantinedObserver>();
        await ReportAlerts();
    }

    [Fact] void should_persist_a_distinct_episode() => _stateStorage.State.QuarantineEpisodeId.ShouldNotEqual(_original);
    [Fact] async Task should_report_both_current_identity_and_the_old_ending() => await _observerAlerts.Received(1).Reconcile(Arg.Is<ObserverAlertSnapshot>(_ => _.IsQuarantined && _.QuarantineEpisodeId != _original && _.Endings[new IncidentId(_original)] == AlertClearedReason.Revived));
}
