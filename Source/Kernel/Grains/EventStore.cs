// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Collections.Immutable;
using Cratis.Chronicle.Grains.Observation.Reducers;
using Cratis.Chronicle.Grains.Projections.Definitions;
using Cratis.Chronicle.Projections.Json;
using Cratis.Chronicle.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Grains;

/// <summary>
/// Represents an implementation of <see cref="IEventStore"/>.
/// </summary>
public class EventStore : IEventStore
{
    readonly ConcurrentDictionary<EventStoreNamespaceName, IEventStoreNamespace> _namespaces = new();
    readonly IServiceProvider _serviceProvider;
    readonly ILoggerFactory _loggerFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="EventStore"/> class.
    /// </summary>
    /// <param name="name">Name of the event store.</param>
    /// <param name="storage"><see cref="IEventStoreStorage"/> for accessing underlying storage for the specific event store.</param>
    /// <param name="serviceProvider"><see cref="IServiceProvider"/> for getting service instances.</param>
    /// <param name="loggerFactory"><see cref="ILoggerFactory"/> for creating loggers.</param>
    public EventStore(
        EventStoreName name,
        IEventStoreStorage storage,
        IServiceProvider serviceProvider,
        ILoggerFactory loggerFactory)
    {
        Name = name;
        Storage = storage;
        _serviceProvider = serviceProvider;
        _loggerFactory = loggerFactory;
        var projectionSerializer = serviceProvider.GetRequiredService<IJsonProjectionSerializer>();
        ProjectionDefinitions = new ProjectionDefinitions(storage.Projections, projectionSerializer);
        ProjectionPipelineDefinitions = new ProjectionPipelineDefinitions(storage.ProjectionPipelines);
        ReducerPipelineDefinitions = new ReducerPipelineDefinitions();
        Namespaces = ImmutableList<IEventStoreNamespace>.Empty;
    }

    /// <inheritdoc/>
    public EventStoreName Name { get; }

    /// <inheritdoc/>
    public IEventStoreStorage Storage { get; }

    /// <inheritdoc/>
    public IProjectionDefinitions ProjectionDefinitions { get; }

    /// <inheritdoc/>
    public IProjectionPipelineDefinitions ProjectionPipelineDefinitions { get; }

    /// <inheritdoc/>
    public IReducerPipelineDefinitions ReducerPipelineDefinitions { get; }

    /// <inheritdoc/>
    public IImmutableList<IEventStoreNamespace> Namespaces { get; private set; }

    /// <inheritdoc/>
    public IEventStoreNamespace GetNamespace(EventStoreNamespaceName @namespace)
    {
        if (!_namespaces.TryGetValue(@namespace, out var namespaceInstance))
        {
            namespaceInstance = new EventStoreNamespace(
                Name,
                @namespace,
                Storage.GetNamespace(@namespace),
                _serviceProvider,
                _loggerFactory);
            _namespaces[@namespace] = namespaceInstance;
            Namespaces = Namespaces.Add(namespaceInstance);
        }

        return namespaceInstance;
    }
}
