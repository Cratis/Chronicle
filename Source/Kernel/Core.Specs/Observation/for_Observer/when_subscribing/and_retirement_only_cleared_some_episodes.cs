// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Alerts;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.for_Observer.when_subscribing;

public class and_retirement_only_cleared_some_episodes : given.an_observer_with_durable_alert_history
{
    Guid _retiredLifecycle;

    async Task Establish()
    {
        await GivenFailingPartitions(200);
        await Catch.Exception(_observer.Retire);
        _retiredLifecycle = _stateStorage.State.AlertLifecycleId;
    }

    async Task Because()
    {
        await _observer.Subscribe<ObserverSubscriber>(ObserverType.Reactor, [], SiloAddress.Zero);
        await ReportAlerts();
    }

    [Fact] void should_start_a_new_lifecycle() => _stateStorage.State.AlertLifecycleId.ShouldNotEqual(_retiredLifecycle);
    [Fact] void should_be_active() => _stateStorage.State.AlertDisposition.ShouldEqual(AlertDisposition.Active);
    [Fact] void should_discard_retired_failure_episodes() => _failedPartitionsStorage.State.Partitions.ShouldBeEmpty();
    [Fact] void should_not_raise_ended_ids_again() => _history.OfType<AlertRaised>().Count().ShouldEqual(200);
    [Fact] void should_finish_clearing_the_retained_history() => _history.OfType<AlertCleared>().Count().ShouldEqual(200);
}
