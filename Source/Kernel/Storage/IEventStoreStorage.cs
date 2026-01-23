// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage.Events.Constraints;
using Cratis.Chronicle.Storage.EventTypes;
using Cratis.Chronicle.Storage.Namespaces;
using Cratis.Chronicle.Storage.Observation;
using Cratis.Chronicle.Storage.Observation.Reactors;
using Cratis.Chronicle.Storage.Observation.Reducers;
using Cratis.Chronicle.Storage.Projections;
using Cratis.Chronicle.Storage.ReadModels;
using Cratis.Chronicle.Storage.Seeding;

namespace Cratis.Chronicle.Storage;

/// <summary>
/// Defines the shared storage for an event store.
/// </summary>
public interface IEventStoreStorage
{
    /// <summary>
    /// Gets the event store the storage represents.
    /// </summary>
    EventStoreName EventStore { get; }

    /// <summary>
    /// Gets the <see cref="INamespaceStorage"/> for the event store.
    /// </summary>
    INamespaceStorage Namespaces { get; }

    /// <summary>
    /// Gets the <see cref="IEventTypesStorage"/> for the event store.
    /// </summary>
    IEventTypesStorage EventTypes { get; }

    /// <summary>
    /// Gets the <see cref="IUniqueConstraintsStorage"/> for the event store.
    /// </summary>
    IConstraintsStorage Constraints { get; }

    /// <summary>
    /// Gets the <see cref="IObserverDefinitionsStorage"/> for the event store.
    /// </summary>
    IObserverDefinitionsStorage Observers { get; }

    /// <summary>
    /// Gets the <see cref="IReactorDefinitionsStorage"/> for the event store.
    /// </summary>
    IReactorDefinitionsStorage Reactors { get; }

    /// <summary>
    /// Gets the <see cref="IReducerDefinitionsStorage"/> for the event store.
    /// </summary>
    IReducerDefinitionsStorage Reducers { get; }

    /// <summary>
    /// Gets the <see cref="IProjectionDefinitionsStorage"/> for the event store.
    /// </summary>
    IProjectionDefinitionsStorage Projections { get; }

    /// <summary>
    /// Gets the <see cref="IReadModelDefinitionsStorage"/> for the event store.
    /// </summary>
    IReadModelDefinitionsStorage ReadModels { get; }

    /// <summary>
    /// Gets the <see cref="IEventSeedingStorage"/> for global event seeding at the event store level.
    /// </summary>
    IEventSeedingStorage EventSeeding { get; }

    /// <summary>
    /// Get a specific <see cref="IEventStoreNamespaceStorage"/> for a <see cref="EventStoreNamespaceName"/>.
    /// </summary>
    /// <param name="namespace">The <see cref="EventStoreNamespaceName"/> to get for.</param>
    /// <returns>The <see cref="IEventStoreNamespaceStorage"/> instance.</returns>
    IEventStoreNamespaceStorage GetNamespace(EventStoreNamespaceName @namespace);
}
