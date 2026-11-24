// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Supervision;

/// <summary>
/// Defines the per-silo supervisor that runs the periodic and departure-driven duties of the grains resident on the silo.
/// </summary>
/// <remarks>
/// A resident grain used to arm a grain timer of its own for each duty: a watchdog every minute, a re-check for the
/// minute after a silo departure, a reconcile loop. The supervisor replaces them with one loop per silo over a registry,
/// so the number of timers no longer grows with the number of observers, namespaces or stores.
/// </remarks>
public interface ISiloSupervisor
{
    /// <summary>
    /// Registers a resident grain. Registering the same key again replaces its registration.
    /// </summary>
    /// <param name="key">The key identifying the registration.</param>
    /// <param name="interval">How often <paramref name="check"/> is due, or null when the grain has no periodic duty.</param>
    /// <param name="check">The periodic duty, returning how long to wait before it is due again, or null for <paramref name="interval"/>. It runs through the grain, so it does not interleave with the grain's other calls.</param>
    /// <param name="afterDeparture">The duty to run, spread over the settle window, after a silo has left the cluster, or null when there is none.</param>
    void Register(string key, TimeSpan? interval, Func<Task<TimeSpan?>>? check, Func<Task>? afterDeparture);

    /// <summary>
    /// Removes a registration. Unknown keys are ignored.
    /// </summary>
    /// <param name="key">The key identifying the registration.</param>
    void Unregister(string key);
}
