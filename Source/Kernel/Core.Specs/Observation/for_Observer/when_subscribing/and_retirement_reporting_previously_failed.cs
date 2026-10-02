// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Alerts;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.for_Observer.when_subscribing;

public class and_retirement_reporting_previously_failed : given.an_observer
{
    Guid _retiredLifecycle;

    async Task Establish()
    {
        FailAlertReports(new TimeoutException());
        await Catch.Exception(_observer.Retire);
        _retiredLifecycle = _stateStorage.State.AlertLifecycleId;
        ApplyAlertReports();
        _observerAlerts.ClearReceivedCalls();
    }

    async Task Because()
    {
        await _observer.Subscribe<ObserverSubscriber>(ObserverType.Reactor, [], SiloAddress.Zero);
        await _observer.PartitionFailed("new", 42UL, ["Failed"], "Stack");
        await _observer.RunWatchdogAsync();
    }

    [Fact] void should_establish_a_new_lifecycle() => _stateStorage.State.AlertLifecycleId.ShouldNotEqual(_retiredLifecycle);
    [Fact] async Task should_report_only_the_live_lifecycle() => await _observerAlerts.Received(1).Reconcile(Arg.Is<ObserverAlertSnapshot>(_ => _.Disposition == AlertDisposition.Active && _.LifecycleId != _retiredLifecycle && _.FailedPartitions.Count == 1));
    [Fact] async Task should_not_dispatch_stale_retirement() => await _observerAlerts.DidNotReceive().Reconcile(Arg.Is<ObserverAlertSnapshot>(_ => _.Disposition != AlertDisposition.Active));
}
