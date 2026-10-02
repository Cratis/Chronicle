// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Alerts;
using Cratis.Chronicle.Concepts.Alerts;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Storage.Observation;
using NSubstitute.Extensions;

namespace Cratis.Chronicle.Observation.for_Observer.when_removing;

public class and_a_timed_out_raise_has_not_committed : given.an_observer_with_durable_alert_history
{
    Exception _firstError;
    ObserverRemovalResult _result;
    bool _deletedBeforeCommit;
    bool _retainedReminder;
    bool _clearedBeforeDeletion;

    async Task Establish()
    {
        await PersistFailingPartitions(1);
        _delayNextRaise = true;
        await ReportAlerts();
        await CrashTracker();
        _systemSequence.DrainAppends().Returns(Task.FromException(new TimeoutException("Append still executing")));
        _eventStoreNamespaceStorage.Observers.Delete(_observerId).Returns(_ =>
        {
            _clearedBeforeDeletion = _history.OfType<AlertCleared>().Any(clear => clear.IncidentId == _pendingRaise.IncidentId);
            return Task.CompletedTask;
        });
    }

    async Task Because()
    {
        _firstError = await Catch.Exception(RemoveThroughCoordinator);
        _deletedBeforeCommit = _eventStoreNamespaceStorage.Observers.ReceivedCalls().Any(call => call.GetMethodInfo().Name == nameof(IObserverStateStorage.Delete));
        _retainedReminder = await _observer.GetReminder(Observer.AlertReminderName) is not null;
        _history.Add(_pendingRaise);
        _systemSequence.Configure().DrainAppends().Returns(Task.CompletedTask);
        _result = await RemoveThroughCoordinator();
    }

    [Fact] void should_refuse_removal_while_the_append_is_uncertain() => _firstError.ShouldBeOfExactType<ObserverAlertsNotReconciled>();
    [Fact] void should_not_delete_the_source_before_the_late_commit() => _deletedBeforeCommit.ShouldBeFalse();
    [Fact] void should_keep_the_observers_durable_wakeup() => _retainedReminder.ShouldBeTrue();
    [Fact] void should_clear_the_late_raise_as_removed() => _history.OfType<AlertCleared>().Single().Reason.ShouldEqual(AlertClearedReason.Removed);
    [Fact] void should_clear_the_incident_before_deleting_the_source() => _clearedBeforeDeletion.ShouldBeTrue();
    [Fact] void should_finish_removal_after_reconciliation() => _result.Outcome.ShouldEqual(ObserverRemovalOutcome.Removed);
    [Fact] void should_keep_the_committed_retired_level() => _stateStorage.State.AlertDisposition.ShouldEqual(AlertDisposition.Retired);
}
