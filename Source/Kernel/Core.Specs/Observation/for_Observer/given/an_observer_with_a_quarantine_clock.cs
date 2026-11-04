// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Observation.given;
using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Observation.for_Observer.given;

public class an_observer_with_a_quarantine_clock : an_observer_with_reloadable_state
{
    protected readonly DateTimeOffset _quarantineTime = new(2026, 1, 2, 3, 4, 5, TimeSpan.Zero);
    protected readonly DeliveryClock _quarantineClock = new();
    readonly blockable_state_storage<ObserverDefinition> _definitionReadStorage = new();

    public an_observer_with_a_quarantine_clock()
    {
        _quarantineClock.Advance(_quarantineTime - DateTimeOffset.UnixEpoch - TimeSpan.FromHours(1));
        _timeProvider = _quarantineClock;
        _silo.Options.StorageFactory = type => type switch
        {
            _ when type == typeof(ObserverState) => _reloadableStateStorage,
            _ when type == typeof(ObserverDefinition) => _definitionReadStorage,
            _ => null!
        };
    }

    protected async Task<ObserverState> ReconcileAndCaptureReloadedState()
    {
        _definitionReadStorage.SuspendNextRead = true;
        var reconciliation = ReconcileSubscription();
        ObserverState reloaded;
        try
        {
            await _definitionReadStorage.ReadStarted.Task.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
            reloaded = await _observer.GetState();
        }
        finally
        {
            _definitionReadStorage.ReleaseRead.TrySetResult();
        }
        await reconciliation.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
        return reloaded;
    }
}
