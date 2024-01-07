// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Aksio.Cratis.Kernel.Compliance;
using Aksio.Cratis.Projections.Json;
using Microsoft.Extensions.Logging;

namespace Aksio.Cratis.Kernel.Storage.MongoDB;

/// <summary>
/// Represents an implementation of <see cref="IStorage"/> for MongoDB.
/// </summary>
public class Storage : IStorage
{
    readonly IDictionary<EventStore, IEventStoreStorage> _eventStores = new Dictionary<EventStore, IEventStoreStorage>();
    readonly IDatabase _database;
    readonly IJsonProjectionSerializer _projectionSerializer;
    readonly IJsonProjectionPipelineSerializer _projectionPipelineSerializer;
    readonly IJsonComplianceManager _complianceManager;
    readonly Json.ExpandoObjectConverter _expandoObjectConverter;
    readonly JsonSerializerOptions _jsonSerializerOptions;
    readonly IExecutionContextManager _executionContextManager;
    readonly ILoggerFactory _loggerFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="Storage"/> class.
    /// </summary>
    /// <param name="database">The MongoDB <see cref="IDatabase"/>.</param>
    /// <param name="projectionSerializer"><see cref="IJsonProjectionSerializer"/> for handling serialization of projection definitions.</param>
    /// <param name="projectionPipelineSerializer"><see cref="IJsonProjectionPipelineSerializer"/> for handling serialization of projection pipeline definitions.</param>
    /// <param name="complianceManager"><see cref="IJsonComplianceManager"/> for handling compliance.</param>
    /// <param name="expandoObjectConverter"><see cref="Json.ExpandoObjectConverter"/> for conversions.</param>
    /// <param name="jsonSerializerOptions">The global <see cref="JsonSerializerOptions"/>.</param>
    /// <param name="executionContextManager"><see cref="IExecutionContextManager"/> for getting the execution context.</param>
    /// <param name="loggerFactory"><see cref="ILoggerFactory"/> for creating loggers.</param>
    public Storage(
        IDatabase database,
        IJsonProjectionSerializer projectionSerializer,
        IJsonProjectionPipelineSerializer projectionPipelineSerializer,
        IJsonComplianceManager complianceManager,
        Json.ExpandoObjectConverter expandoObjectConverter,
        JsonSerializerOptions jsonSerializerOptions,
        IExecutionContextManager executionContextManager,
        ILoggerFactory loggerFactory)
    {
        _database = database;
        _projectionSerializer = projectionSerializer;
        _projectionPipelineSerializer = projectionPipelineSerializer;
        _complianceManager = complianceManager;
        _expandoObjectConverter = expandoObjectConverter;
        _jsonSerializerOptions = jsonSerializerOptions;
        _executionContextManager = executionContextManager;
        _loggerFactory = loggerFactory;
    }

    /// <inheritdoc/>
    public IEventStoreStorage GetEventStore(EventStore eventStore)
    {
        if (_eventStores.TryGetValue(eventStore, out var storage))
        {
            return storage;
        }

        return _eventStores[eventStore] = new EventStoreStorage(
            eventStore,
            _database,
            _database.GetEventStoreDatabase(eventStore),
            _projectionSerializer,
            _projectionPipelineSerializer,
            _complianceManager,
            _expandoObjectConverter,
            _jsonSerializerOptions,
            _executionContextManager,
            _loggerFactory);
    }
}
