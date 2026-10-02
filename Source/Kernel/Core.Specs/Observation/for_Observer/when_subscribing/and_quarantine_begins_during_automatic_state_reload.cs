// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.States;

namespace Cratis.Chronicle.Observation.for_Observer.when_subscribing;

public class and_quarantine_begins_during_automatic_state_reload : given.an_observer_with_reloadable_state
{
    bool _wasQuarantinedBeforeReactivation;
    ObserverRunningState _persistedStateBeforeReactivation;

    async Task Establish()
    {
        await _stateStorage.WriteStateAsync();
        _reloadableStateStorage.PersistedState.RunningState.ShouldEqual(ObserverRunningState.Active);
        _reloadableStateStorage.SuspendNextRead = true;
    }

    async Task Because()
    {
        var reconciliation = ReconcileSubscription();
        try
        {
            await _reloadableStateStorage.ReadStarted.Task.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
            await _observer.FailedPartitionRecovered(_partition, 42UL);
        }
        finally
        {
            _reloadableStateStorage.ReleaseRead.SetResult();
        }
        await reconciliation.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
        _wasQuarantinedBeforeReactivation = await _observer.GetCurrentState() is QuarantinedObserver;
        _persistedStateBeforeReactivation = _reloadableStateStorage.PersistedState.RunningState;
        await Reactivate();
    }

    [Fact] void should_keep_quarantine_in_memory_before_reactivation() => _wasQuarantinedBeforeReactivation.ShouldBeTrue();
    [Fact] void should_persist_quarantine_before_reactivation() => _persistedStateBeforeReactivation.ShouldEqual(ObserverRunningState.Quarantined);
    [Fact] async Task should_reactivate_in_quarantine() => (await _observer.GetCurrentState()).ShouldBeOfExactType<QuarantinedObserver>();
    [Fact] void should_keep_the_persisted_quarantine_after_reactivation() => _stateStorage.State.RunningState.ShouldEqual(ObserverRunningState.Quarantined);
    [Fact] void should_not_resume_jobs() => ShouldNotResumeJobs();
    [Fact] void should_not_start_replay() => ShouldNotStartReplay();
    [Fact] void should_not_start_catchup() => ShouldNotStartCatchup();
    [Fact] void should_not_resubscribe_to_the_queue() => ShouldNotSubscribeToQueue();
}
