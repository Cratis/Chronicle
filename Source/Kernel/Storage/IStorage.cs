// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Storage;

/// <summary>
/// Defines the storage for the cluster level.
/// </summary>
public interface IStorage
{
    /// <summary>
    /// Gets the system storage.
    /// </summary>
    ISystemStorage System { get; }

    /// <summary>
    /// Reads floor-bearing stores independently of the resettable registry, without provisioning storage.
    /// </summary>
    /// <returns>The durable floors by store.</returns>
    /// <exception cref="NotSupportedException">The provider has not implemented this optional capability.</exception>
    Task<IReadOnlyDictionary<EventStoreName, Concepts.Projections.Generations.Enablement.GenerationFloor>> ReadGenerationFloors() => throw new NotSupportedException("The provider does not support read-only generation floor discovery.");

    /// <summary>
    /// Moves the System event store's data to a <see cref="SystemEventStoreLocation"/>.
    /// </summary>
    /// <param name="location">The <see cref="SystemEventStoreLocation"/> to move to.</param>
    /// <returns>Awaitable task.</returns>
    /// <remarks>
    /// Only a provider that derives where the System event store is kept from configuration has anything to move. A
    /// provider that keeps it in the same place regardless has nothing to do, which is what the default does.
    /// </remarks>
    Task MoveSystemEventStoreTo(SystemEventStoreLocation location) => Task.CompletedTask;

    /// <summary>
    /// Finds System event store data that sits where this kernel is not looking.
    /// </summary>
    /// <returns>A description of where the data is and where the kernel looks, or null when nothing is misplaced.</returns>
    /// <remarks>
    /// A kernel configured to look at one set of System databases while the data sits in the other (an environment
    /// variable that pins the old name after the databases moved, or an older release) would otherwise start against an
    /// empty store and fail somewhere unrelated. A provider that keeps the System event store in one place has nothing to find.
    /// </remarks>
    Task<string?> FindMisplacedSystemEventStoreData() => Task.FromResult<string?>(null);

    /// <summary>
    /// Finds System event store data left behind under its previous name after it was moved.
    /// </summary>
    /// <returns>A description of what remains and what to do about it, or null when nothing remains.</returns>
    /// <remarks>
    /// Moving the System event store copies its data and then drops the source, which some installations do not grant
    /// the kernel's user the rights to do. The kernel then runs from the moved data while the stale source remains,
    /// which is harmless until someone points the kernel back at it. A provider that keeps the System event store in
    /// one place has nothing to find.
    /// </remarks>
    Task<string?> FindRetainedLegacySystemEventStoreData() => Task.FromResult<string?>(null);

    /// <summary>
    /// Get all the <see cref="EventStoreName">event stores</see> available.
    /// </summary>
    /// <returns>Collection of <see cref="EventStoreName"/>.</returns>
    Task<IEnumerable<EventStoreName>> GetEventStores();

    /// <summary>
    /// Checks if an event store exists.
    /// </summary>
    /// <param name="eventStore"><see cref="EventStoreName"/> to check.</param>
    /// <returns>True if the event store exists, false otherwise.</returns>
    Task<bool> HasEventStore(EventStoreName eventStore);

    /// <summary>
    /// Observes all the <see cref="EventStoreName">event stores</see> available.
    /// </summary>
    /// <returns>Collection of <see cref="EventStoreName"/>.</returns>
    ISubject<IEnumerable<EventStoreName>> ObserveEventStores();

    /// <summary>
    /// Get the <see cref="IEventStoreStorage"/> for a specific <see cref="EventStoreName"/>.
    /// </summary>
    /// <param name="eventStore"><see cref="EventStoreName"/> to get.</param>
    /// <returns>The <see cref="IEventStoreStorage"/> instance.</returns>
    IEventStoreStorage GetEventStore(EventStoreName eventStore);

    /// <summary>
    /// Evicts every cached <see cref="IEventStoreStorage"/> so the next <see cref="GetEventStore"/>
    /// call reconstructs them — and the per-namespace sinks they own — from scratch.
    /// </summary>
    /// <remarks>
    /// Intended for integration-test boundaries where the database is wiped between tests.
    /// Sinks keep in-memory state (bulk-mode flags, in-replay flags on the underlying
    /// collections, per-key state caches) that the database wipe does not touch; without
    /// evicting them, the next test inherits whatever state the previous test left behind —
    /// for example a sink stuck in replay mode silently buffers writes into the temporary
    /// replay collection while the test reads from the real one and sees nothing.
    /// </remarks>
    void Clear();
}
