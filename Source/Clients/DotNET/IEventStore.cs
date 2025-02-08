// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Aggregates;
using Cratis.Chronicle.Connections;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Events.Constraints;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Projections;
using Cratis.Chronicle.Reactors;
using Cratis.Chronicle.Reducers;
using Cratis.Chronicle.Transactions;

namespace Cratis.Chronicle;

/// <summary>
/// Defines the event store API surface.
/// </summary>
public interface IEventStore
{
    /// <summary>
    /// Gets the <see cref="Name"/> for the event store.
    /// </summary>
    EventStoreName Name { get; }

    /// <summary>
    /// Gets the namespace for the event store.
    /// </summary>
    EventStoreNamespaceName Namespace { get; }

    /// <summary>
    /// Gets the <see cref="IChronicleConnection"/> used for the <see cref="IEventStore"/>.
    /// </summary>
    IChronicleConnection Connection { get; }

    /// <summary>
    /// Gets the <see cref="IClientArtifactsProvider"/> for the event store.
    /// </summary>
    IUnitOfWorkManager UnitOfWorkManager { get; }

    /// <summary>
    /// Gets the <see cref="IAggregateRootFactory"/>.
    /// </summary>
    IAggregateRootFactory AggregateRootFactory { get; }

    /// <summary>
    /// Gets the <see cref="IEventTypes"/> for the event store.
    /// </summary>
    IEventTypes EventTypes { get; }

    /// <summary>
    /// Gets the <see cref="IConstraints"/> for the event store.
    /// </summary>
    IConstraints Constraints { get; }

    /// <summary>
    /// Gets the <see cref="IEventLog"/> event sequence.
    /// </summary>
    IEventLog EventLog { get; }

    /// <summary>
    /// Gets the <see cref="IReactors"/> for the event store.
    /// </summary>
    IReactors Reactors { get; }

    /// <summary>
    /// Gets the <see cref="IReducers"/> for the event store.
    /// </summary>
    IReducers Reducers { get; }

    /// <summary>
    /// Gets the <see cref="IProjections"/> for the event store.
    /// </summary>
    IProjections Projections { get; }

    /// <summary>
    /// Discover all artifacts for the event store.
    /// </summary>
    /// <returns>Awaitable task.</returns>
    Task DiscoverAll();

    /// <summary>
    /// Register all artifacts for the event store.
    /// </summary>
    /// <returns>Awaitable task.</returns>
    Task RegisterAll();

    /// <summary>
    /// Get an event sequence by id.
    /// </summary>
    /// <param name="id">The identifier of the event sequence to get.</param>
    /// <returns><see cref="IEventSequence"/> instance.</returns>
    IEventSequence GetEventSequence(EventSequenceId id);

    /// <summary>
    /// List namespaces in the event store.
    /// </summary>
    /// <param name="cancellationToken">Optional <see cref="CancellationToken"/>.</param>
    /// <returns>An asynchronous enumerable for all namespace names.</returns>
    Task<IEnumerable<EventStoreNamespaceName>> GetNamespaces(CancellationToken cancellationToken = default);
}
