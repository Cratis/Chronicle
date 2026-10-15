// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Storage.Observation;
using Cratis.Orleans.Jobs;

namespace Cratis.Chronicle.Observation.for_Observer.given;

public class an_observer_with_stale_state_and_a_pending_subscription_read : an_observer_with_reloadable_state
{
    readonly blockable_state_storage<ObserverDefinition> _definitionReadStorage = new();
    readonly blockable_state_storage<FailedPartitions> _failuresReadStorage = new();
    protected ObserverRunningState _runningStateWhileReadIsPending;
    protected ObserverRunningState _persistedStateAfterCompletion;

    protected virtual bool BlockDefinitionRead => true;

    public an_observer_with_stale_state_and_a_pending_subscription_read()
    {
        _silo.Options.StorageFactory = type => type switch
        {
            _ when type == typeof(ObserverState) => _reloadableStateStorage,
            _ when type == typeof(ObserverDefinition) => _definitionReadStorage,
            _ when type == typeof(FailedPartitions) => _failuresReadStorage,
            _ => null!
        };
    }

    async Task Establish()
    {
        await _stateStorage.WriteStateAsync();
        _reloadableStateStorage.PersistedState.RunningState.ShouldEqual(ObserverRunningState.Active);
        _reloadableStateStorage.SuspendNextRead = true;
        if (BlockDefinitionRead)
        {
            _definitionReadStorage.SuspendNextRead = true;
        }
        else
        {
            _failuresReadStorage.SuspendNextRead = true;
        }
    }

    protected async Task CompleteCatchupDuringFollowupRead()
    {
        var reconciliation = ReconcileSubscription();
        var readStarted = BlockDefinitionRead ? _definitionReadStorage.ReadStarted : _failuresReadStorage.ReadStarted;
        try
        {
            await _reloadableStateStorage.ReadStarted.Task.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
            await _observer.FailedPartitionRecovered(_partition, 42UL);
            _reloadableStateStorage.ReleaseRead.SetResult();
            await readStarted.Task.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
            _runningStateWhileReadIsPending = _stateStorage.State.RunningState;
            await _observer.CaughtUp(JobId.NotSet, 84UL);
            _persistedStateAfterCompletion = _reloadableStateStorage.PersistedState.RunningState;
        }
        finally
        {
            _reloadableStateStorage.ReleaseRead.TrySetResult();
            _definitionReadStorage.ReleaseRead.TrySetResult();
            _failuresReadStorage.ReleaseRead.TrySetResult();
        }
        await reconciliation.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
    }
}
