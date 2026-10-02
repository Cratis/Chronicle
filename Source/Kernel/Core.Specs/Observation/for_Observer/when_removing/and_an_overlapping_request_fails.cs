// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Alerts;
using Cratis.Chronicle.Concepts.Observation;
using NSubstitute.Extensions;

namespace Cratis.Chronicle.Observation.for_Observer.when_removing;

public class and_an_overlapping_request_fails : given.an_observer_with_durable_alert_history
{
    readonly TaskCompletionSource _entered = new();
    readonly TaskCompletionSource _release = new();
    Exception _secondError;
    ObserverRemovalResult _firstResult;

    async Task Establish()
    {
        await GivenFailingPartitions(1);
        _observerAlerts.Configure().Reconcile(Arg.Any<ObserverAlertSnapshot>()).Returns(async call =>
        {
            _entered.TrySetResult();
            await _release.Task;
            return await _tracker.Reconcile(call.Arg<ObserverAlertSnapshot>());
        });
    }

    async Task Because()
    {
        var first = RemoveThroughCoordinator();
        await _entered.Task.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);

        // TestKit allows direct overlap, more interleaving than the observer's normal serialized grain calls.
        _secondError = await Catch.Exception(RemoveThroughCoordinator);
        _release.SetResult();
        _firstResult = await first;
    }

    [Fact] void should_fail_the_unacknowledged_request() => _secondError.ShouldBeOfExactType<ObserverAlertsNotReconciled>();
    [Fact] void should_complete_the_original_request() => _firstResult.Outcome.ShouldEqual(ObserverRemovalOutcome.Removed);
    [Fact] void should_never_restore_active_state() => _stateStorage.State.AlertDisposition.ShouldEqual(AlertDisposition.Retired);
    [Fact] void should_not_reopen_the_incident() => _history.OfType<AlertRaised>().Count().ShouldEqual(1);
    [Fact] void should_clear_the_incident_once() => _history.OfType<AlertCleared>().Count().ShouldEqual(1);
}
