// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Text.Json;
using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Projections.Json;
using Cratis.Chronicle.Storage.EventTypes;
using Cratis.Chronicle.Storage.Identities;
using Cratis.Chronicle.Storage.MongoDB.Identities;
using Cratis.Chronicle.Storage.MongoDB.Namespaces;
using Cratis.Chronicle.Storage.MongoDB.Projections;
using Cratis.Chronicle.Storage.Namespaces;
using Cratis.Chronicle.Storage.Projections;
using Cratis.Events.MongoDB.EventTypes;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Storage.MongoDB;

/// <summary>
/// Represents an implementation of <see cref="IEventStoreStorage"/> for MongoDB.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="EventStoreStorage"/> class.
/// </remarks>
/// <param name="eventStore"><see cref="EventStore"/> the storage is for.</param>
/// <param name="database"><see cref="IDatabase"/> to use.</param>
/// <param name="eventStoreDatabase"><see cref="IEventStoreDatabase"/> to use.</param>
/// <param name="projectionSerializer"><see cref="IJsonProjectionSerializer"/> for handling serialization of projection definitions.</param>
/// <param name="projectionPipelineSerializer"><see cref="IJsonProjectionPipelineSerializer"/> for handling serialization of projection pipeline definitions.</param>
/// <param name="complianceManager"><see cref="IJsonComplianceManager"/> for handling compliance.</param>
/// <param name="expandoObjectConverter"><see cref="Json.IExpandoObjectConverter"/> for conversions.</param>
/// <param name="jsonSerializerOptions">The global <see cref="JsonSerializerOptions"/>.</param>
/// <param name="loggerFactory"><see cref="ILoggerFactory"/> for creating loggers.</param>
public class EventStoreStorage(
    EventStoreName eventStore,
    IDatabase database,
    IEventStoreDatabase eventStoreDatabase,
    IJsonProjectionSerializer projectionSerializer,
    IJsonProjectionPipelineSerializer projectionPipelineSerializer,
    IJsonComplianceManager complianceManager,
    Json.IExpandoObjectConverter expandoObjectConverter,
    JsonSerializerOptions jsonSerializerOptions,
    ILoggerFactory loggerFactory) : IEventStoreStorage
{
    readonly ConcurrentDictionary<EventStoreNamespaceName, IEventStoreNamespaceStorage> _namespaces = new();

    /// <inheritdoc/>
    public EventStoreName EventStore { get; } = eventStore;

    /// <inheritdoc/>
    public INamespaceStorage Namespaces { get; } = new NamespaceStorage(eventStoreDatabase, loggerFactory.CreateLogger<NamespaceStorage>());

    /// <inheritdoc/>
    public IIdentityStorage Identities { get; } = new IdentityStorage(database, loggerFactory.CreateLogger<IdentityStorage>());

    /// <inheritdoc/>
    public IEventTypesStorage EventTypes { get; } = new EventTypesStorage(eventStore, eventStoreDatabase, loggerFactory.CreateLogger<EventTypesStorage>());

    /// <inheritdoc/>
    public IProjectionDefinitionsStorage Projections { get; } = new ProjectionDefinitionsStorage(eventStoreDatabase, projectionSerializer);

    /// <inheritdoc/>
    public IProjectionPipelineDefinitionsStorage ProjectionPipelines { get; } = new ProjectionPipelineDefinitionsStorage(eventStoreDatabase, projectionPipelineSerializer);

    /// <inheritdoc/>
    public IEventStoreNamespaceStorage GetNamespace(EventStoreNamespaceName @namespace)
    {
        if (_namespaces.TryGetValue(@namespace, out var instance))
        {
            return instance;
        }

        var converter = new EventConverter(
            EventStore,
            @namespace,
            EventTypes,
            Identities,
            complianceManager,
            expandoObjectConverter);

        return _namespaces[@namespace] =
            new EventStoreNamespaceStorage(
                EventStore,
                @namespace,
                eventStoreDatabase.GetNamespaceDatabase(@namespace),
                converter,
                EventTypes,
                expandoObjectConverter,
                jsonSerializerOptions,
                loggerFactory);
    }
}
