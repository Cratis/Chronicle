// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.States;

namespace Cratis.Chronicle.Observation.for_Observer.when_subscribing;

public class and_catchup_completes_during_definition_reload_after_a_stale_snapshot : given.an_observer_with_stale_state_and_a_pending_subscription_read
{
    async Task Because() => await CompleteCatchupDuringFollowupRead();

    [Fact] void should_restore_quarantine_before_the_definition_read() => _runningStateWhileReadIsPending.ShouldEqual(ObserverRunningState.Quarantined);
    [Fact] void should_persist_quarantine_during_completion() => _persistedStateAfterCompletion.ShouldEqual(ObserverRunningState.Quarantined);
    [Fact] async Task should_keep_the_quarantined_state() => (await _observer.GetCurrentState()).ShouldBeOfExactType<QuarantinedObserver>();
    [Fact] void should_keep_the_persisted_quarantine() => _reloadableStateStorage.PersistedState.RunningState.ShouldEqual(ObserverRunningState.Quarantined);
    [Fact] void should_record_completion_progress() => _reloadableStateStorage.PersistedState.NextEventSequenceNumber.ShouldEqual((EventSequenceNumber)85UL);
    [Fact] void should_not_resume_jobs() => ShouldNotResumeJobs();
    [Fact] void should_not_start_replay() => ShouldNotStartReplay();
    [Fact] void should_not_start_catchup() => ShouldNotStartCatchup();
    [Fact] void should_not_resubscribe_to_the_queue() => ShouldNotSubscribeToQueue();
}
