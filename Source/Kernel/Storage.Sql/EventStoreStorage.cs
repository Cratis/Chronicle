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
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Chronicle.Storage.Sql;

/// <summary>
/// Represents an implementation of <see cref="IEventStoreStorage"/> for a specific event store.
/// </summary>
/// <param name="serviceProvider">The service provider.</param>
/// <param name="eventStore">The name of the event store.</param>
public class EventStoreStorage(IServiceProvider serviceProvider, EventStoreName eventStore) : IEventStoreStorage
{
    /// <inheritdoc/>
    public EventStoreName EventStore { get; } = eventStore;

    /// <inheritdoc/>
    public INamespaceStorage Namespaces { get; } = new Namespaces.NamespaceStorage(serviceProvider.GetRequiredService<EventStoreDbContext>());

    /// <inheritdoc/>
    public IEventTypesStorage EventTypes { get; } = new EventTypes.EventTypesStorage(eventStore, serviceProvider.GetRequiredService<EventStoreDbContext>());

    /// <inheritdoc/>
    public IConstraintsStorage Constraints => throw new NotImplementedException();

    /// <inheritdoc/>
    public IObserverDefinitionsStorage Observers => throw new NotImplementedException();

    /// <inheritdoc/>
    public IReactorDefinitionsStorage Reactors { get; } = new Reactors.ReactorDefinitionsStorage(serviceProvider.GetRequiredService<EventStoreDbContext>());

    /// <inheritdoc/>
    public IReducerDefinitionsStorage Reducers { get; } = new Reducers.ReducerDefinitionsStorage(serviceProvider.GetRequiredService<EventStoreDbContext>());

    /// <inheritdoc/>
    public IProjectionDefinitionsStorage Projections => new Projections.ProjectionDefinitionsStorage(serviceProvider.GetRequiredService<EventStoreDbContext>());

    /// <inheritdoc/>
    public IReadModelDefinitionsStorage ReadModels => new ReadModels.ReadModelDefinitionsStorage(serviceProvider.GetRequiredService<EventStoreDbContext>());

    /// <inheritdoc/>
    public IEventStoreNamespaceStorage GetNamespace(EventStoreNamespaceName @namespace) => throw new NotImplementedException();
}
