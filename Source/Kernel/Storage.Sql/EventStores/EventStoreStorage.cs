// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Jobs;
using Cratis.Chronicle.Storage.Events.Constraints;
using Cratis.Chronicle.Storage.EventTypes;
using Cratis.Chronicle.Storage.Namespaces;
using Cratis.Chronicle.Storage.Observation;
using Cratis.Chronicle.Storage.Observation.Reactors;
using Cratis.Chronicle.Storage.Observation.Reducers;
using Cratis.Chronicle.Storage.Observation.Webhooks;
using Cratis.Chronicle.Storage.Projections;
using Cratis.Chronicle.Storage.ReadModels;
using Cratis.Chronicle.Storage.Sinks;
using Cratis.Types;

namespace Cratis.Chronicle.Storage.Sql.EventStores;

/// <summary>
/// Represents an implementation of <see cref="IEventStoreStorage"/> for a specific event store.
/// </summary>
/// <param name="eventStore">The name of the event store.</param>
/// <param name="database">The <see cref="IDatabase"/> to use for storage operations.</param>
/// <param name="sinkFactories"><see cref="IInstancesOf{T}"/> for getting all <see cref="ISinkFactory"/> instances.</param>
/// <param name="jobTypes">The <see cref="IJobTypes"/> that knows about job types.</param>
/// <param name="jsonSerializerOptions">The global <see cref="JsonSerializerOptions"/>.</param>
public class EventStoreStorage(EventStoreName eventStore, IDatabase database, IInstancesOf<ISinkFactory> sinkFactories, IJobTypes jobTypes, JsonSerializerOptions jsonSerializerOptions) : IEventStoreStorage
{
    /// <inheritdoc/>
    public EventStoreName EventStore { get; } = eventStore.Value;

    /// <inheritdoc/>
    public INamespaceStorage Namespaces { get; } = new Namespaces.NamespaceStorage(eventStore, database);

    /// <inheritdoc/>
    public IEventTypesStorage EventTypes { get; } = new EventTypes.EventTypesStorage(eventStore, database);

    /// <inheritdoc/>
    public IConstraintsStorage Constraints { get; } = new Constraints.ConstraintsStorage(eventStore, database);

    /// <inheritdoc/>
    public IObserverDefinitionsStorage Observers { get; } = new Observers.ObserverDefinitionsStorage(eventStore, database);

    /// <inheritdoc/>
    public IReactorDefinitionsStorage Reactors { get; } = new Reactors.ReactorDefinitionsStorage(eventStore, database);

    /// <inheritdoc/>
    public IReducerDefinitionsStorage Reducers { get; } = new Reducers.ReducerDefinitionsStorage(eventStore, database);

    /// <inheritdoc/>
    public IProjectionDefinitionsStorage Projections { get; } = new Projections.ProjectionDefinitionsStorage(eventStore, database);

    /// <inheritdoc/>
    public IWebhookDefinitionsStorage Webhooks { get; } = new Webhooks.WebhookDefinitionsStorage(eventStore, database);

    /// <inheritdoc/>
    public IReadModelDefinitionsStorage ReadModels { get; } = new ReadModels.ReadModelDefinitionsStorage(eventStore, database);

    /// <inheritdoc/>
    public IEventStoreNamespaceStorage GetNamespace(EventStoreNamespaceName @namespace)
        => new Namespaces.EventStoreNamespaceStorage(eventStore, @namespace, database, sinkFactories, jobTypes, Observers, jsonSerializerOptions);
}
