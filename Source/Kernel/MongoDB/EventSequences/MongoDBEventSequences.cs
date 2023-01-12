// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Text.Json;
using Aksio.Cratis.DependencyInversion;
using Aksio.Cratis.Events;
using Aksio.Cratis.EventSequences;
using Aksio.Cratis.Execution;
using Aksio.Cratis.Json;
using Aksio.Cratis.Schemas;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Aksio.Cratis.Kernel.MongoDB;

/// <summary>
/// Represents an implementation of <see cref="IEventSequences"/> for MongoDB.
/// </summary>
public class MongoDBEventSequences : IEventSequences
{
    readonly ILogger<MongoDBEventSequences> _logger;
    readonly IExecutionContextManager _executionContextManager;
    readonly ProviderFor<IEventStoreDatabase> _eventStoreDatabaseProvider;
    readonly ProviderFor<ISchemaStore> _schemaStoreProvider;
    readonly IExpandoObjectConverter _expandoObjectConverter;
    readonly JsonSerializerOptions _jsonSerializerOptions;

    /// <summary>
    /// Initializes a new instance of <see cref="MongoDBEventSequences"/>.
    /// </summary>
    /// <param name="executionContextManager"><see cref="IExecutionContextManager"/> for getting current <see cref="ExecutionContext"/>.</param>
    /// <param name="eventStoreDatabaseProvider"><see cref="ProviderFor{T}">Provider for</see> <see cref="IMongoDatabase"/>.</param>
    /// <param name="schemaStoreProvider">The <see cref="ISchemaStore"/> for working with the schema types.</param>
    /// <param name="expandoObjectConverter"><see cref="IExpandoObjectConverter"/> for converting between expando object and json objects.</param>
    /// <param name="jsonSerializerOptions">The global <see cref="JsonSerializerOptions"/>.</param>
    /// <param name="logger"><see cref="ILogger"/> for logging.</param>
    public MongoDBEventSequences(
        IExecutionContextManager executionContextManager,
        ProviderFor<IEventStoreDatabase> eventStoreDatabaseProvider,
        ProviderFor<ISchemaStore> schemaStoreProvider,
        IExpandoObjectConverter expandoObjectConverter,
        JsonSerializerOptions jsonSerializerOptions,
        ILogger<MongoDBEventSequences> logger)
    {
        _eventStoreDatabaseProvider = eventStoreDatabaseProvider;
        _schemaStoreProvider = schemaStoreProvider;
        _expandoObjectConverter = expandoObjectConverter;
        _jsonSerializerOptions = jsonSerializerOptions;
        _logger = logger;
        _executionContextManager = executionContextManager;
    }

    /// <inheritdoc/>
    public async Task Append(
        EventSequenceId eventSequenceId,
        EventSequenceNumber sequenceNumber,
        EventSourceId eventSourceId,
        EventType eventType,
        DateTimeOffset validFrom,
        ExpandoObject content)
    {
        try
        {
            var schema = await _schemaStoreProvider().GetFor(eventType.Id, eventType.Generation);
            var jsonObject = _expandoObjectConverter.ToJsonObject(content, schema.Schema);
            var document = BsonDocument.Parse(JsonSerializer.Serialize(jsonObject, _jsonSerializerOptions));
            _logger.Appending(
                sequenceNumber,
                eventSequenceId,
                _executionContextManager.Current.MicroserviceId,
                _executionContextManager.Current.TenantId);
            var @event = new Event(
                sequenceNumber,
                _executionContextManager.Current.CorrelationId,
                _executionContextManager.Current.CausationId,
                _executionContextManager.Current.CausedBy,
                eventType.Id,
                DateTimeOffset.UtcNow,
                validFrom,
                eventSourceId,
                new Dictionary<string, BsonDocument>
                {
                        { eventType.Generation.ToString(), document }
                },
                Array.Empty<EventCompensation>());
            var collection = GetCollectionFor(eventSequenceId);
            await collection.InsertOneAsync(@event);
        }
        catch (Exception ex)
        {
            _logger.AppendFailure(
                sequenceNumber,
                eventSequenceId,
                _executionContextManager.Current.MicroserviceId,
                _executionContextManager.Current.TenantId,
                ex);
            throw;
        }
    }

    /// <inheritdoc/>
    public Task Compensate(
        EventSequenceId eventSequenceId,
        EventSequenceNumber sequenceNumber,
        EventType eventType,
        DateTimeOffset validFrom,
        ExpandoObject content) => throw new NotImplementedException();

    IMongoCollection<Event> GetCollectionFor(EventSequenceId eventSequenceId) => _eventStoreDatabaseProvider().GetEventSequenceCollectionFor(eventSequenceId);
}
