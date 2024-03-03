// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Events;
using Cratis.EventSequences;
using Cratis.Observation;
using Cratis.Projections;
using Cratis.Reducers;

namespace Cratis;

/// <summary>
/// Defines the event store API surface.
/// </summary>
public interface IEventStore
{
    /// <summary>
    /// Gets the <see cref="EventStoreName"/> for the event store.
    /// </summary>
    EventStoreName EventStoreName { get; }

    /// <summary>
    /// Gets the namespace for the event store.
    /// </summary>
    EventStoreNamespaceName Namespace { get; }

    /// <summary>
    /// Gets the <see cref="ICratisConnection"/> used for the <see cref="IEventStore"/>.
    /// </summary>
    ICratisConnection Connection { get; }

    /// <summary>
    /// Gets the <see cref="IEventTypes"/> for the event store.
    /// </summary>
    IEventTypes EventTypes { get; }

    /// <summary>
    /// Gets the <see cref="IEventLog"/> event sequence.
    /// </summary>
    IEventLog EventLog { get; }

    /// <summary>
    /// Gets the <see cref="IObservers"/> for the event store.
    /// </summary>
    IObservers Observers { get; }

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
}
