// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Projections.Json;
using Cratis.Chronicle.Storage.Sinks;
using Cratis.Types;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Storage.MongoDB;

/// <summary>
/// Represents an implementation of <see cref="IStorage"/> for MongoDB.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="Storage"/> class.
/// </remarks>
/// <param name="database">The MongoDB <see cref="IDatabase"/>.</param>
/// <param name="projectionSerializer"><see cref="IJsonProjectionSerializer"/> for handling serialization of projection definitions.</param>
/// <param name="complianceManager"><see cref="IJsonComplianceManager"/> for handling compliance.</param>
/// <param name="expandoObjectConverter"><see cref="Json.IExpandoObjectConverter"/> for conversions.</param>
/// <param name="jsonSerializerOptions">The global <see cref="JsonSerializerOptions"/>.</param>
/// <param name="sinkFactories"><see cref="IInstancesOf{T}"/> for getting all <see cref="ISinkFactory"/> instances.</param>
/// <param name="loggerFactory"><see cref="ILoggerFactory"/> for creating loggers.</param>
public class Storage(
    IDatabase database,
    IJsonProjectionSerializer projectionSerializer,
    IJsonComplianceManager complianceManager,
    Json.IExpandoObjectConverter expandoObjectConverter,
    JsonSerializerOptions jsonSerializerOptions,
    IInstancesOf<ISinkFactory> sinkFactories,
    ILoggerFactory loggerFactory) : IStorage
{
    readonly Dictionary<EventStoreName, IEventStoreStorage> _eventStores = [];

    /// <inheritdoc/>
    public IEventStoreStorage GetEventStore(EventStoreName eventStore)
    {
        if (_eventStores.TryGetValue(eventStore, out var storage))
        {
            return storage;
        }

        return _eventStores[eventStore] = new EventStoreStorage(
            eventStore,
            database,
            database.GetEventStoreDatabase(eventStore),
            projectionSerializer,
            complianceManager,
            expandoObjectConverter,
            jsonSerializerOptions,
            sinkFactories,
            loggerFactory);
    }
}
