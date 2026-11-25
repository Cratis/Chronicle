// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Observation.for_Observer.given;

public class an_observer_whose_state_write_can_be_held : an_observer
{
    protected readonly blockable_observer_state_write _stateWrite = new();

    public an_observer_whose_state_write_can_be_held() =>
        _silo.Options.StorageFactory = type => type == typeof(ObserverState) ? _stateWrite : null!;

    async Task Establish() =>
        await _observer.Subscribe<NullObserverSubscriber>(ObserverType.Reactor, [], SiloAddress.Zero);

    /// <summary>
    /// Starts a deactivation and holds its state write in flight, the way a slow store does during a rolling upgrade.
    /// </summary>
    /// <param name="reason">Why the observer deactivates.</param>
    /// <param name="cancellation">The token the runtime cancels when the deactivation times out.</param>
    /// <returns>The running deactivation.</returns>
    protected async Task<Task> DeactivateWithTheWriteInFlight(DeactivationReason reason, CancellationToken cancellation)
    {
        _stateWrite.Block = true;
        var deactivation = _observer.OnDeactivateAsync(reason, cancellation);
        await _stateWrite.Writing.Task.WaitAsync(TimeSpan.FromSeconds(10), TimeProvider.System);
        return deactivation;
    }
}
