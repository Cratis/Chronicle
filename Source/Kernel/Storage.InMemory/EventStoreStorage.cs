// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Jobs;
using Cratis.Chronicle.Storage.Events.Constraints;
using Cratis.Chronicle.Storage.EventTypes;
using Cratis.Chronicle.Storage.InMemory.Events.Constraints;
using Cratis.Chronicle.Storage.InMemory.Events.EventTypes;
using Cratis.Chronicle.Storage.InMemory.Namespaces;
using Cratis.Chronicle.Storage.InMemory.Observation;
using Cratis.Chronicle.Storage.InMemory.Observation.EventStoreSubscriptions;
using Cratis.Chronicle.Storage.InMemory.Observation.Reactors;
using Cratis.Chronicle.Storage.InMemory.Observation.Reducers;
using Cratis.Chronicle.Storage.InMemory.Observation.Webhooks;
using Cratis.Chronicle.Storage.InMemory.Projections;
using Cratis.Chronicle.Storage.InMemory.ReadModels;
using Cratis.Chronicle.Storage.InMemory.Seeding;
using Cratis.Chronicle.Storage.Namespaces;
using Cratis.Chronicle.Storage.Observation;
using Cratis.Chronicle.Storage.Observation.EventStoreSubscriptions;
using Cratis.Chronicle.Storage.Observation.Reactors;
using Cratis.Chronicle.Storage.Observation.Reducers;
using Cratis.Chronicle.Storage.Observation.Webhooks;
using Cratis.Chronicle.Storage.Projections;
using Cratis.Chronicle.Storage.ReadModels;
using Cratis.Chronicle.Storage.Seeding;
using Cratis.Chronicle.Storage.Sinks;

namespace Cratis.Chronicle.Storage.InMemory;

/// <summary>
/// Represents an in-memory implementation of <see cref="IEventStoreStorage"/>.
/// </summary>
/// <param name="eventStore">The <see cref="EventStoreName"/> the storage is for.</param>
/// <param name="sinksFactory">Factory delegate that creates <see cref="ISinks"/> for a namespace.</param>
/// <param name="jobTypes">The <see cref="IJobTypes"/> for resolving job state types.</param>
public sealed class EventStoreStorage(
    EventStoreName eventStore,
    SinksFactory sinksFactory,
    IJobTypes jobTypes) : IEventStoreStorage
{
    readonly ConcurrentDictionary<EventStoreNamespaceName, IEventStoreNamespaceStorage> _namespaces = new();

    /// <inheritdoc/>
    public EventStoreName EventStore { get; } = eventStore;

    /// <inheritdoc/>
    public INamespaceStorage Namespaces { get; } = new NamespaceStorage();

    /// <inheritdoc/>
    public IEventTypesStorage EventTypes { get; } = new EventTypesStorage();

    /// <inheritdoc/>
    public IConstraintsStorage Constraints { get; } = new ConstraintsStorage();

    /// <inheritdoc/>
    public IObserverDefinitionsStorage Observers { get; } = new ObserverDefinitionsStorage();

    /// <inheritdoc/>
    public IReactorDefinitionsStorage Reactors { get; } = new ReactorDefinitionsStorage();

    /// <inheritdoc/>
    public IReducerDefinitionsStorage Reducers { get; } = new ReducerDefinitionsStorage();

    /// <inheritdoc/>
    public IProjectionDefinitionsStorage Projections { get; } = new ProjectionDefinitionsStorage();

    /// <inheritdoc/>
    public IWebhookDefinitionsStorage Webhooks { get; } = new WebhookDefinitionsStorage();

    /// <inheritdoc/>
    public IEventStoreSubscriptionDefinitionsStorage EventStoreSubscriptions { get; } = new EventStoreSubscriptionDefinitionsStorage();

    /// <inheritdoc/>
    public IReadModelDefinitionsStorage ReadModels { get; } = new ReadModelDefinitionsStorage();

    /// <inheritdoc/>
    public IEventSeedingStorage EventSeeding { get; } = new EventSeedingStorage();

    /// <inheritdoc/>
    public IEventStoreNamespaceStorage GetNamespace(EventStoreNamespaceName @namespace)
    {
        if (_namespaces.TryGetValue(@namespace, out var existing))
        {
            return existing;
        }

        var created = new EventStoreNamespaceStorage(EventStore, @namespace, jobTypes, sinksFactory(@namespace));
        return _namespaces.GetOrAdd(@namespace, created);
    }
}
