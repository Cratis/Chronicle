// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Alerts;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.for_Observer.when_removing;

public class and_more_incidents_are_open_than_one_report_can_clear : given.an_observer_with_durable_alert_history
{
    Exception _firstError;
    ObserverRemovalResult _result;
    int _firstClearCount;

    async Task Establish() => await GivenFailingPartitions(200);

    async Task Because()
    {
        _firstError = await Catch.Exception(RemoveThroughCoordinator);
        _firstClearCount = _history.OfType<AlertCleared>().Count();
        await _observer.ReceiveReminder(Observer.AlertReminderName, default);
        _result = await RemoveThroughCoordinator();
    }

    [Fact] void should_report_incomplete_reconciliation() => _firstError.ShouldBeOfExactType<ObserverAlertsNotReconciled>();
    [Fact] void should_bound_the_first_clear_batch() => _firstClearCount.ShouldEqual(128);
    [Fact] void should_complete_on_retry() => _result.Outcome.ShouldEqual(ObserverRemovalOutcome.Removed);
    [Fact] void should_raise_each_episode_only_once() => _history.OfType<AlertRaised>().GroupBy(alert => alert.IncidentId).All(group => group.Count() == 1).ShouldBeTrue();
    [Fact] void should_clear_every_episode() => _history.OfType<AlertCleared>().Count().ShouldEqual(200);
    [Fact] void should_clear_each_episode_only_once() => _history.OfType<AlertCleared>().GroupBy(alert => alert.IncidentId).All(group => group.Count() == 1).ShouldBeTrue();
    [Fact] void should_not_restore_active_state() => _stateStorage.State.AlertDisposition.ShouldEqual(AlertDisposition.Retired);
}
