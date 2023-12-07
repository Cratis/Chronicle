// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Dynamic;
using System.Text.Json;
using Aksio.Cratis.Auditing;
using Aksio.Cratis.Events;
using Aksio.Cratis.EventSequences;
using Aksio.Cratis.Identities;
using Aksio.Cratis.Kernel.EventSequences;
using Aksio.Cratis.Kernel.Grains.EventSequences;
using Aksio.Cratis.Kernel.Schemas;
using Aksio.DependencyInversion;
using Aksio.Strings;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Aksio.Cratis.Kernel.MongoDB.EventSequences;

#pragma warning disable CA1849, MA0042 // MongoDB breaks the Orleans task model internally, so it won't return to the task scheduler

/// <summary>
/// Represents an implementation of <see cref="IEventSequenceStorage"/> for MongoDB.
/// </summary>
public class MongoDBEventSequenceStorage : IEventSequenceStorage
{
    readonly IExecutionContextManager _executionContextManager;
    readonly ProviderFor<IEventConverter> _converterProvider;
    readonly ProviderFor<IEventStoreDatabase> _eventStoreDatabaseProvider;
    readonly ProviderFor<ISchemaStore> _schemaStoreProvider;
    readonly Json.IExpandoObjectConverter _expandoObjectConverter;
    readonly JsonSerializerOptions _jsonSerializerOptions;
    readonly ILogger<MongoDBEventSequenceStorage> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="MongoDBEventSequenceStorage"/> class.
    /// </summary>
    /// <param name="executionContextManager"><see cref="IExecutionContextManager"/> for getting current <see cref="ExecutionContext"/>.</param>
    /// <param name="converterProvider"><see cref="IEventConverter"/> to convert event types.</param>
    /// <param name="eventStoreDatabaseProvider">Provider for <see cref="IEventStoreDatabase"/> to use.</param>
    /// <param name="schemaStoreProvider">The <see cref="ISchemaStore"/> for working with the schema types.</param>
    /// <param name="expandoObjectConverter"><see cref="IExpandoObjectConverter"/> for converting between expando object and json objects.</param>
    /// <param name="jsonSerializerOptions">The global <see cref="JsonSerializerOptions"/>.</param>
    /// <param name="logger"><see cref="ILogger"/> for logging.</param>
    public MongoDBEventSequenceStorage(
        IExecutionContextManager executionContextManager,
        ProviderFor<IEventConverter> converterProvider,
        ProviderFor<IEventStoreDatabase> eventStoreDatabaseProvider,
        ProviderFor<ISchemaStore> schemaStoreProvider,
        Json.IExpandoObjectConverter expandoObjectConverter,
        JsonSerializerOptions jsonSerializerOptions,
        ILogger<MongoDBEventSequenceStorage> logger)
    {
        _executionContextManager = executionContextManager;
        _converterProvider = converterProvider;
        _eventStoreDatabaseProvider = eventStoreDatabaseProvider;
        _schemaStoreProvider = schemaStoreProvider;
        _expandoObjectConverter = expandoObjectConverter;
        _jsonSerializerOptions = jsonSerializerOptions;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<EventCount> GetCount(
        EventSequenceId eventSequenceId,
        EventSequenceNumber? lastEventSequenceNumber = null,
        IEnumerable<EventType>? eventTypes = null)
    {
        var filters = new List<FilterDefinition<Event>>();
        if (lastEventSequenceNumber is not null)
        {
            filters.Add(Builders<Event>.Filter.Lte(_ => _.SequenceNumber, lastEventSequenceNumber.Value));
        }
        if (eventTypes?.Any() ?? false)
        {
            filters.Add(Builders<Event>.Filter.In(e => e.Type, eventTypes.Select(_ => _.Id).ToArray()));
        }
        if (filters.Count == 0)
        {
            filters.Add(FilterDefinition<Event>.Empty);
        }

        var filter = Builders<Event>.Filter.And(filters.ToArray());
        var collection = GetCollectionFor(eventSequenceId);
        return await collection.CountDocumentsAsync(filter).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task Append(
        EventSequenceId eventSequenceId,
        EventSequenceNumber sequenceNumber,
        EventSourceId eventSourceId,
        EventType eventType,
        IEnumerable<Causation> causation,
        IEnumerable<IdentityId> causedByChain,
        DateTimeOffset occurred,
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
                causation,
                causedByChain,
                eventType.Id,
                occurred,
                validFrom,
                eventSourceId,
                new Dictionary<string, BsonDocument>
                {
                        { eventType.Generation.ToString(), document }
                },
                Array.Empty<EventCompensation>());
            var collection = GetCollectionFor(eventSequenceId);
            await collection.InsertOneAsync(@event).ConfigureAwait(false);
        }
        catch (MongoWriteException writeException) when (writeException.WriteError.Category == ServerErrorCategory.DuplicateKey)
        {
            _logger.DuplicateEventSequenceNumber(
                sequenceNumber,
                eventSequenceId,
                _executionContextManager.Current.MicroserviceId,
                _executionContextManager.Current.TenantId);

            throw new DuplicateEventSequenceNumber(sequenceNumber, eventSequenceId);
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
        IEnumerable<Causation> causation,
        IEnumerable<IdentityId> causedByChain,
        DateTimeOffset occurred,
        DateTimeOffset validFrom,
        ExpandoObject content) => throw new NotImplementedException();

    /// <inheritdoc/>
    public async Task<AppendedEvent> Redact(
        EventSequenceId eventSequenceId,
        EventSequenceNumber sequenceNumber,
        RedactionReason reason,
        IEnumerable<Causation> causation,
        IEnumerable<IdentityId> causedByChain,
        DateTimeOffset occurred)
    {
        _logger.Redacting(eventSequenceId, sequenceNumber);
        var collection = GetCollectionFor(eventSequenceId);

        var @event = await GetEventAt(eventSequenceId, sequenceNumber);
        if (@event.Metadata.Type == GlobalEventTypes.Redaction)
        {
            _logger.RedactionAlreadyApplied(eventSequenceId, sequenceNumber);
            return @event;
        }

        var updateModel = CreateRedactionUpdateModelFor(@event, reason, causation, causedByChain, occurred);
        await collection.UpdateOneAsync(updateModel.Filter, updateModel.Update).ConfigureAwait(false);

        return @event;
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<EventType>> Redact(
        EventSequenceId eventSequenceId,
        EventSourceId eventSourceId,
        RedactionReason reason,
        IEnumerable<EventType>? eventTypes,
        IEnumerable<Causation> causation,
        IEnumerable<IdentityId> causedByChain,
        DateTimeOffset occurred)
    {
        _logger.RedactingMultiple(eventSequenceId, eventSourceId, eventTypes ?? Enumerable.Empty<EventType>());
        var collection = GetCollectionFor(eventSequenceId);
        var updates = new List<UpdateOneModel<Event>>();
        var affectedEventTypes = new HashSet<EventType>();

        using var cursor = await GetFromSequenceNumber(eventSequenceId, EventSequenceNumber.First, eventSourceId, eventTypes);
        while (await cursor.MoveNext())
        {
            foreach (var @event in cursor.Current)
            {
                if (@event.Metadata.Type.Id == GlobalEventTypes.Redaction)
                {
                    _logger.RedactionAlreadyApplied(eventSequenceId, @event.Metadata.SequenceNumber);
                    continue;
                }

                updates.Add(CreateRedactionUpdateModelFor(@event, reason, causation, causedByChain, occurred));
                affectedEventTypes.Add(@event.Metadata.Type);
            }

            await collection.BulkWriteAsync(updates).ConfigureAwait(false);
        }

        return affectedEventTypes;
    }

    /// <inheritdoc/>
    public async Task<EventSequenceNumber> GetHeadSequenceNumber(
        EventSequenceId eventSequenceId,
        IEnumerable<EventType>? eventTypes = null,
        EventSourceId? eventSourceId = null)
    {
        _logger.GettingHeadSequenceNumber(eventSequenceId);

        var collection = GetCollectionFor(eventSequenceId);
        var filters = new List<FilterDefinition<Event>>();
        if (eventTypes?.Any() ?? false)
        {
            filters.Add(Builders<Event>.Filter.In(e => e.Type, eventTypes.Select(_ => _.Id).ToArray()));
        }

        if (eventSourceId?.IsSpecified == true)
        {
            filters.Add(Builders<Event>.Filter.Eq(e => e.EventSourceId, eventSourceId));
        }

        var filter = Builders<Event>.Filter.And(filters.ToArray());

        var highest = await collection.Find(filter)
                                      .SortBySequenceNumber()
                                      .Limit(1)
                                      .SingleOrDefaultAsync()
                                      .ConfigureAwait(false);
        return highest?.SequenceNumber ?? EventSequenceNumber.Unavailable;
    }

    /// <inheritdoc/>
    public async Task<EventSequenceNumber> GetTailSequenceNumber(
        EventSequenceId eventSequenceId,
        IEnumerable<EventType>? eventTypes = null,
        EventSourceId? eventSourceId = null)
    {
        _logger.GettingTailSequenceNumber(eventSequenceId);

        var collection = GetCollectionFor(eventSequenceId);
        var filters = new List<FilterDefinition<Event>>();
        if (eventTypes?.Any() ?? false)
        {
            filters.Add(Builders<Event>.Filter.In(e => e.Type, eventTypes.Select(_ => _.Id).ToArray()));
        }
        if (eventSourceId?.IsSpecified == true)
        {
            filters.Add(Builders<Event>.Filter.Eq(e => e.EventSourceId, eventSourceId));
        }
        if (filters.Count == 0)
        {
            filters.Add(FilterDefinition<Event>.Empty);
        }

        var filter = Builders<Event>.Filter.And(filters.ToArray());
        var highest = await collection.Find(filter)
                                      .SortByDescendingSequenceNumber()
                                      .Limit(1)
                                      .SingleOrDefaultAsync()
                                      .ConfigureAwait(false);
        return highest?.SequenceNumber ?? EventSequenceNumber.Unavailable;
    }

    /// <inheritdoc/>
    public Task<TailEventSequenceNumbers> GetTailSequenceNumbers(
        EventSequenceId eventSequenceId,
        IEnumerable<EventType> eventTypes)
    {
        var collection = _eventStoreDatabaseProvider().GetEventSequenceCollectionAsBsonFor(eventSequenceId);
        var sort = PipelineStageDefinitionBuilder.Sort<BsonDocument>(/*lang=json*/ "{ '_id' : -1 }");
        var limit = PipelineStageDefinitionBuilder.Limit<BsonDocument>(1);
        var tailProjection = PipelineStageDefinitionBuilder.Project<BsonDocument>(/*lang=json*/ $"{{ '{nameof(TailEventSequenceNumbers.Tail)}': '$_id'}}");
        var tailPipeline = PipelineDefinition<BsonDocument, BsonDocument>.Create(new[] { sort, limit, tailProjection });
        var tailFacet = AggregateFacet.Create("tail", tailPipeline);

        var filter = Builders<BsonDocument>.Filter.In(nameof(Event.Type).ToCamelCase(), eventTypes.Select(_ => _.Id));
        var tailForEventTypesMatch = PipelineStageDefinitionBuilder.Match(filter);
        var tailForEventTypesProjection = PipelineStageDefinitionBuilder.Project<BsonDocument>(/*lang=json*/ $"{{ '{nameof(TailEventSequenceNumbers.TailForEventTypes)}': '$_id'}}");
        var tailForEventTypesPipeline = PipelineDefinition<BsonDocument, BsonDocument>.Create(new[] { tailForEventTypesMatch, sort, limit, tailForEventTypesProjection });
        var tailForEventTypesFacet = AggregateFacet.Create("tailForEventTypes", tailForEventTypesPipeline);

        var sequenceNumbers = collection
            .Aggregate()
            .Facet(tailFacet, tailForEventTypesFacet)
            .Project<BsonDocument>($"{{'{nameof(TailEventSequenceNumbers.Tail)}': {{ '$arrayElemAt': ['$tail.{nameof(TailEventSequenceNumbers.Tail)}',0] }}, '{nameof(TailEventSequenceNumbers.TailForEventTypes)}': {{ '$arrayElemAt': ['$tail2.{nameof(TailEventSequenceNumbers.TailForEventTypes)}',0] }} }}")
            .Single();

        var hasTail = sequenceNumbers.TryGetValue("Tail", out var tail) && tail is not null && tail is not BsonNull;
        var hasTailForEventTypes = sequenceNumbers.TryGetValue("TailForEventTypes", out var tailForEventTypes) && tailForEventTypes is not null && tailForEventTypes is not BsonNull;

        return Task.FromResult(new TailEventSequenceNumbers(
            eventSequenceId,
            eventTypes.ToImmutableList(),
            hasTail ? new EventSequenceNumber((ulong)tail!.AsInt64) : EventSequenceNumber.Unavailable,
            hasTailForEventTypes ? new EventSequenceNumber((ulong)tailForEventTypes!.AsInt64) : EventSequenceNumber.Unavailable));
    }

    /// <inheritdoc/>
    public async Task<IImmutableDictionary<EventType, EventSequenceNumber>> GetTailSequenceNumbersForEventTypes(EventSequenceId eventSequenceId, IEnumerable<EventType> eventTypes)
    {
        var eventTypeIds = eventTypes.Select(_ => _.Id.Value).ToArray();
        _logger.GettingTailSequenceNumbersForEventTypes(eventSequenceId, eventTypeIds);

        var collection = _eventStoreDatabaseProvider().GetEventSequenceCollectionAsBsonFor(eventSequenceId);

        var eventTypesFilter = Builders<BsonDocument>.Filter.In(new StringFieldDefinition<BsonDocument, Guid>(nameof(Event.Type).ToCamelCase()), eventTypeIds);
        var sortDefinition = Builders<BsonDocument>.Sort.Descending("_id");
        var groupDefinitions = new BsonDocument
        {
            { "_id", "$type" },
            { "items", new BsonDocument("$push", "$_id") }
        };
        var projectDefinition = new BsonDocument(
            "items", new BsonDocument(
                "$slice", new BsonArray
                {
                    "$items",
                    0,
                    1
                }));

        var matchStage = PipelineStageDefinitionBuilder.Match(eventTypesFilter);
        var sortStage = PipelineStageDefinitionBuilder.Sort(sortDefinition);
        var groupStage = PipelineStageDefinitionBuilder.Group<BsonDocument, Guid>(groupDefinitions);
        var projectStage = PipelineStageDefinitionBuilder.Project<BsonDocument>(projectDefinition);

        var aggregation = collection.Aggregate()
                  .Match(eventTypesFilter)
                  .Sort(sortDefinition)
                  .Group(groupDefinitions)
                  .Project(projectDefinition);

        var result = await aggregation.ToListAsync().ConfigureAwait(false);
        var resultAsDictionary = eventTypes.ToDictionary(_ => _, _ => EventSequenceNumber.Unavailable);
        foreach (var item in result)
        {
            var eventType = eventTypes.FirstOrDefault(_ => _.Id == (EventTypeId)item["_id"].AsGuid);
            if (eventType != null)
            {
                resultAsDictionary[eventType] = new EventSequenceNumber((ulong)item["items"][0].AsInt64);
            }
        }

        return resultAsDictionary.ToImmutableDictionary();
    }

    /// <inheritdoc/>
    public async Task<EventSequenceNumber> GetNextSequenceNumberGreaterOrEqualThan(
        EventSequenceId eventSequenceId,
        EventSequenceNumber sequenceNumber,
        IEnumerable<EventType>? eventTypes = null,
        EventSourceId? eventSourceId = null)
    {
        var collection = _eventStoreDatabaseProvider().GetEventSequenceCollectionFor(eventSequenceId);
        var filters = new List<FilterDefinition<Event>>
            {
                Builders<Event>.Filter.Gte(_ => _.SequenceNumber, sequenceNumber.Value)
            };

        if (eventTypes?.Any() ?? false)
        {
            filters.Add(Builders<Event>.Filter.In(e => e.Type, eventTypes.Select(_ => _.Id).ToArray()));
        }
        if (eventSourceId?.IsSpecified == true)
        {
            filters.Add(Builders<Event>.Filter.Eq(e => e.EventSourceId, eventSourceId));
        }

        var filter = Builders<Event>.Filter.And(filters.ToArray());
        var highest = await collection.Find(filter)
                                      .SortBySequenceNumber()
                                      .Limit(1)
                                      .SingleOrDefaultAsync()
                                      .ConfigureAwait(false);
        return highest?.SequenceNumber ?? EventSequenceNumber.Unavailable;
    }

    /// <inheritdoc/>
    public async Task<bool> HasInstanceFor(EventSequenceId eventSequenceId, EventTypeId eventTypeId, EventSourceId eventSourceId)
    {
        var filter = Builders<Event>.Filter.And(
            Builders<Event>.Filter.Eq(_ => _.Type, eventTypeId),
            Builders<Event>.Filter.Eq(_ => _.EventSourceId, eventSourceId));

        var collection = GetCollectionFor(eventSequenceId);
        var count = await collection.Find(filter)
                                    .CountDocumentsAsync()
                                    .ConfigureAwait(false);
        return count > 0;
    }

    /// <inheritdoc/>
    public async Task<AppendedEvent> GetEventAt(EventSequenceId eventSequenceId, EventSequenceNumber sequenceNumber)
    {
        _logger.GettingEventAtSequenceNumber(eventSequenceId, sequenceNumber);

        var filter = Builders<Event>.Filter.And(Builders<Event>.Filter.Eq(_ => _.SequenceNumber, sequenceNumber));

        var collection = GetCollectionFor(eventSequenceId);
        var @event = await collection.Find(filter)
                                     .SortByDescendingSequenceNumber()
                                     .Limit(1)
                                     .SingleAsync()
                                     .ConfigureAwait(false);
        return await _converterProvider().ToAppendedEvent(@event);
    }

    /// <inheritdoc/>
    public async Task<AppendedEvent> GetLastInstanceFor(
        EventSequenceId eventSequenceId,
        EventTypeId eventTypeId,
        EventSourceId eventSourceId)
    {
        _logger.GettingLastInstanceFor(eventSequenceId, eventTypeId, eventSourceId);

        var filter = Builders<Event>.Filter.And(
            Builders<Event>.Filter.Eq(_ => _.Type, eventTypeId),
            Builders<Event>.Filter.Eq(_ => _.EventSourceId, eventSourceId));

        var collection = GetCollectionFor(eventSequenceId);
        var @event = await collection.Find(filter)
                                     .SortByDescendingSequenceNumber()
                                     .Limit(1)
                                     .SingleOrDefaultAsync()
                                     .ConfigureAwait(false) ?? throw new MissingEvent(eventSequenceId, eventTypeId, eventSourceId);
        return await _converterProvider().ToAppendedEvent(@event);
    }

    /// <inheritdoc/>
    public async Task<AppendedEvent> GetLastInstanceOfAny(
        EventSequenceId eventSequenceId,
        EventSourceId eventSourceId,
        IEnumerable<EventTypeId> eventTypes)
    {
        _logger.GettingLastInstanceOfAny(eventSequenceId, eventSourceId, eventTypes);

        var anyEventTypes = Builders<Event>.Filter.In(e => e.Type, eventTypes);

        var filter = Builders<Event>.Filter.And(
            anyEventTypes,
            Builders<Event>.Filter.Eq(_ => _.EventSourceId, eventSourceId));

        var collection = GetCollectionFor(eventSequenceId);
        var @event = await collection.Find(filter)
                                     .SortByDescendingSequenceNumber()
                                     .Limit(1)
                                     .SingleAsync()
                                     .ConfigureAwait(false);
        return await _converterProvider().ToAppendedEvent(@event);
    }

    /// <inheritdoc/>
    public async Task<IEventCursor> GetFromSequenceNumber(
        EventSequenceId eventSequenceId,
        EventSequenceNumber sequenceNumber,
        EventSourceId? eventSourceId = null,
        IEnumerable<EventType>? eventTypes = null,
        CancellationToken cancellationToken = default)
    {
        _logger.GettingFromSequenceNumber(eventSequenceId, sequenceNumber);

        var collection = GetCollectionFor(eventSequenceId);
        var filters = new List<FilterDefinition<Event>>
            {
                Builders<Event>.Filter.Gte(_ => _.SequenceNumber, sequenceNumber.Value)
            };

        if (eventSourceId?.IsSpecified == true)
        {
            filters.Add(Builders<Event>.Filter.Eq(e => e.EventSourceId, eventSourceId));
        }

        if (eventTypes?.Any() == true)
        {
            filters.Add(Builders<Event>.Filter.In(e => e.Type, eventTypes.Select(_ => _.Id).ToArray()));
        }

        var filter = Builders<Event>.Filter.And(filters.ToArray());
        var cursor = await collection.Find(filter)
                                     .SortBySequenceNumber()
                                     .ToCursorAsync(cancellationToken)
                                     .ConfigureAwait(false);
        return new EventCursor(_converterProvider(), cursor);
    }

    /// <inheritdoc/>
    public async Task<IEventCursor> GetRange(
        EventSequenceId eventSequenceId,
        EventSequenceNumber start,
        EventSequenceNumber end,
        EventSourceId? eventSourceId = default,
        IEnumerable<EventType>? eventTypes = default,
        CancellationToken cancellationToken = default)
    {
        _logger.GettingRange(eventSequenceId, start, end);
        var collection = GetCollectionFor(eventSequenceId);
        var filters = new List<FilterDefinition<Event>>
            {
                Builders<Event>.Filter.Gte(_ => _.SequenceNumber, start.Value),
                Builders<Event>.Filter.Lte(_ => _.SequenceNumber, end.Value)
            };

        if (eventSourceId?.IsSpecified == true)
        {
            filters.Add(Builders<Event>.Filter.Eq(e => e.EventSourceId, eventSourceId));
        }

        if (eventTypes?.Any() == true)
        {
            filters.Add(Builders<Event>.Filter.In(e => e.Type, eventTypes.Select(_ => _.Id).ToArray()));
        }

        var filter = Builders<Event>.Filter.And(filters.ToArray());

        var cursor = await collection.Find(filter)
                                     .SortBySequenceNumber()
                                     .ToCursorAsync(cancellationToken)
                                     .ConfigureAwait(false);
        return new EventCursor(_converterProvider(), cursor);
    }

    IMongoCollection<Event> GetCollectionFor(EventSequenceId eventSequenceId) => _eventStoreDatabaseProvider().GetEventSequenceCollectionFor(eventSequenceId);

    UpdateOneModel<Event> CreateRedactionUpdateModelFor(
        AppendedEvent @event,
        RedactionReason reason,
        IEnumerable<Causation> causation,
        IEnumerable<IdentityId> causedById,
        DateTimeOffset occurred)
    {
        var executionContext = _executionContextManager.Current;
        var content = new RedactionEventContent(
            reason,
            @event.Metadata.Type.Id,
            occurred,
            executionContext.CorrelationId,
            causation,
            causedById);

        var document = BsonDocument.Parse(JsonSerializer.Serialize(content, _jsonSerializerOptions));
        var generationalContent = new Dictionary<string, BsonDocument>
                {
                        { EventGeneration.First.ToString(), document }
                };

        return new UpdateOneModel<Event>(
            Builders<Event>.Filter.Eq(e => e.SequenceNumber, @event.Metadata.SequenceNumber),
            Builders<Event>.Update
                .Set(e => e.Type, GlobalEventTypes.Redaction)
                .Set(e => e.Content, generationalContent));
    }
}
