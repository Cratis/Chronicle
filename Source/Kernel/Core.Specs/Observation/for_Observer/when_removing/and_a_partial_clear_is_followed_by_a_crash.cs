// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Alerts;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.for_Observer.when_removing;

public class and_a_partial_clear_is_followed_by_a_crash : given.an_observer_with_durable_alert_history
{
    ObserverRemovalResult _result;

    async Task Establish()
    {
        await GivenFailingPartitions(3);
        _loseNextClearResponse = true;
        await Catch.Exception(RemoveThroughCoordinator);
    }

    async Task Because()
    {
        await CrashTracker();
        await Crash();
        await _observer.ReceiveReminder(Observer.AlertReminderName, default);
        _result = await RemoveThroughCoordinator();
    }

    [Fact] void should_complete_removal() => _result.Outcome.ShouldEqual(ObserverRemovalOutcome.Removed);
    [Fact] void should_keep_committed_retirement() => _stateStorage.State.AlertDisposition.ShouldEqual(AlertDisposition.Retired);
    [Fact] void should_not_raise_old_ids_again() => _history.OfType<AlertRaised>().Count().ShouldEqual(3);
    [Fact] void should_not_repeat_the_committed_clear() => _history.OfType<AlertCleared>().Count().ShouldEqual(3);
    [Fact] void should_clear_distinct_incidents() => _history.OfType<AlertCleared>().Select(alert => alert.IncidentId).Distinct().Count().ShouldEqual(3);
}
