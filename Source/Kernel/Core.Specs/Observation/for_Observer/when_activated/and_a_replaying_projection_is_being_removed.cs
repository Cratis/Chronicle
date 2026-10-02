// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Alerts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.Jobs;
using Cratis.Chronicle.Observation.States;

namespace Cratis.Chronicle.Observation.for_Observer.when_activated;

public class and_a_replaying_projection_is_being_removed : given.a_replaying_projection
{
    async Task Establish()
    {
        await _observer.Remove();
        _jobsManager.ClearReceivedCalls();
        _observerHandledCountsStorage.ClearReceivedCalls();
        _observerAlerts.ClearReceivedCalls();
    }

    async Task Because()
    {
        await Crash();
        await _observer.ReceiveReminder(Observer.AlertReminderName, default);
    }

    [Fact] async Task should_stay_disconnected() => (await _observer.GetCurrentState()).ShouldBeOfExactType<Disconnected>();
    [Fact] void should_retain_replay() => _stateStorage.State.IsReplaying.ShouldBeTrue();
    [Fact] void should_retain_handled_count() => _stateStorage.State.HandledEventCount.ShouldEqual((EventCount)42);
    [Fact] void should_retain_per_type_count() => _stateStorage.State.HandledEventCountPerEventType["event"].ShouldEqual((EventCount)42);
    [Fact] async Task should_not_start_replay() => await _jobsManager.DidNotReceive().Start<IReplayObserver, ReplayObserverRequest>(Arg.Any<ReplayObserverRequest>());
    [Fact] async Task should_not_resume_replay() => await _jobsManager.DidNotReceive().Resume(_replayJob);
    [Fact] async Task should_not_remove_counts() => await _observerHandledCountsStorage.DidNotReceive().RemoveAllFor(Arg.Any<ObserverId>());
    [Fact] async Task should_report_removal() => await _observerAlerts.Received(1).Reconcile(Arg.Is<ObserverAlertSnapshot>(_ => _.Disposition == AlertDisposition.Removing));
}
