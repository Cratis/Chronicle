// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.States;

namespace Cratis.Chronicle.Observation.for_Observer.when_subscribing;

public class and_quarantine_begins_during_reload_of_missing_state : given.an_observer_with_reloadable_state
{
    async Task Establish()
    {
        await _reloadableStateStorage.ClearStateAsync();
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
    }

    [Fact] async Task should_preserve_the_new_quarantine() => (await _observer.GetCurrentState()).ShouldBeOfExactType<QuarantinedObserver>();
    [Fact] void should_persist_the_new_quarantine() => _reloadableStateStorage.PersistedState.RunningState.ShouldEqual(ObserverRunningState.Quarantined);
    [Fact] void should_restore_the_activation_identity() => _reloadableStateStorage.PersistedState.Identifier.ShouldEqual(_observerId);
    [Fact] void should_not_resume_jobs() => ShouldNotResumeJobs();
    [Fact] void should_not_resubscribe_to_the_queue() => ShouldNotSubscribeToQueue();
}
