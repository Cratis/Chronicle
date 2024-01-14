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
using Aksio.Cratis.Kernel.Storage.EventSequences;
using Aksio.Cratis.Kernel.Storage.EventTypes;
using Aksio.Strings;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Aksio.Cratis.Kernel.Storage.MongoDB.EventSequences;

#pragma warning disable CA1849, MA0042 // MongoDB breaks the Orleans task model internally, so it won't return to the task scheduler

/// <summary>
/// Represents an implementation of <see cref="IEventSequenceStorage"/> for MongoDB.
/// </summary>
public class EventSequenceStorage : IEventSequenceStorage
{
    readonly IExecutionContextManager _executionContextManager;
    readonly EventStoreName _eventStore;
    readonly EventStoreNamespaceName _namespace;
    readonly EventSequenceId _eventSequenceId;
    readonly IEventConverter _converter;
    readonly IEventStoreNamespaceDatabase _database;
    readonly IEventTypesStorage _eventTypesStorage;
    readonly Json.IExpandoObjectConverter _expandoObjectConverter;
    readonly JsonSerializerOptions _jsonSerializerOptions;
    readonly ILogger<EventSequenceStorage> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="EventSequenceStorage"/> class.
    /// </summary>
    /// <param name="eventStore"><see cref="EventStoreName"/> the storage is for.</param>
    /// <param name="namespace"><see cref="EventStoreNamespaceName"/> the storage is for.</param>
    /// <param name="eventSequenceId">The <see cref="EventSequenceId"/> the storage represent.</param>
    /// <param name="database">Provider for <see cref="IEventStoreNamespaceDatabase"/> to use.</param>
    /// <param name="converter"><see cref="IEventConverter"/> to convert event types.</param>
    /// <param name="eventTypesStorage">The <see cref="IEventTypesStorage"/> for working with the schema types.</param>
    /// <param name="expandoObjectConverter"><see cref="IExpandoObjectConverter"/> for converting between expando object and json objects.</param>
    /// <param name="jsonSerializerOptions">The global <see cref="JsonSerializerOptions"/>.</param>
    /// <param name="executionContextManager"><see cref="IExecutionContextManager"/> for getting the execution context.</param>
    /// <param name="logger"><see cref="ILogger"/> for logging.</param>
    public EventSequenceStorage(
        EventStoreName eventStore,
        EventStoreNamespaceName @namespace,
        EventSequenceId eventSequenceId,
        IEventStoreNamespaceDatabase database,
        IEventConverter converter,
        IEventTypesStorage eventTypesStorage,
        Json.IExpandoObjectConverter expandoObjectConverter,
        JsonSerializerOptions jsonSerializerOptions,
        IExecutionContextManager executionContextManager,
        ILogger<EventSequenceStorage> logger)
    {
        _executionContextManager = executionContextManager;
        _eventStore = eventStore;
        _namespace = @namespace;
        _eventSequenceId = eventSequenceId;
        _converter = converter;
        _database = database;
        _eventTypesStorage = eventTypesStorage;
        _expandoObjectConverter = expandoObjectConverter;
        _jsonSerializerOptions = jsonSerializerOptions;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<Kernel.Storage.EventSequences.EventSequenceState> GetState()
    {
        var collection = _database.GetCollection<EventSequenceState>(WellKnownCollectionNames.EventSequences);
        var filter = Builders<EventSequenceState>.Filter.Eq(new StringFieldDefinition<EventSequenceState, Guid>("_id"), _eventSequenceId);
        var cursor = await collection.FindAsync(filter).ConfigureAwait(false);
        var state = await cursor.FirstOrDefaultAsync();
        return state?.ToKernel() ?? new Kernel.Storage.EventSequences.EventSequenceState();
    }

    /// <inheritdoc/>
    public async Task SaveState(Kernel.Storage.EventSequences.EventSequenceState state)
    {
        var collection = _database.GetCollection<EventSequenceState>(WellKnownCollectionNames.EventSequences);
        var filter = Builders<EventSequenceState>.Filter.Eq(new StringFieldDefinition<EventSequenceState, Guid>("_id"), _eventSequenceId);

        await collection.ReplaceOneAsync(filter, state.ToMongoDB(), new ReplaceOptions { IsUpsert = true }).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<EventCount> GetCount(
        EventSequenceNumber? lastEventSequenceNumber = null,
        IEnumerable<EventType>? eventTypes = null)
    {
        var filters = new List<FilterDefinition<Event>>();
        if (lastEventSequenceNumber is not null)
        {
            filters.Add(Builders<Event>.Filter.Lte(_ => _.SequenceNumber, lastEventSequenceNumber));
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
        var collection = GetCollection();
        return await collection.CountDocumentsAsync(filter).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task Append(
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
            var schema = await _eventTypesStorage.GetFor(eventType.Id, eventType.Generation);
            var jsonObject = _expandoObjectConverter.ToJsonObject(content, schema.Schema);
            var document = BsonDocument.Parse(JsonSerializer.Serialize(jsonObject, _jsonSerializerOptions));
            _logger.Appending(
                sequenceNumber,
                _eventSequenceId,
                _eventStore,
                _namespace);
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
            var collection = GetCollection();
            await collection.InsertOneAsync(@event).ConfigureAwait(false);
        }
        catch (MongoWriteException writeException) when (writeException.WriteError.Category == ServerErrorCategory.DuplicateKey)
        {
            _logger.DuplicateEventSequenceNumber(
                sequenceNumber,
                _eventSequenceId,
                _eventStore,
                _namespace);

            throw new DuplicateEventSequenceNumber(sequenceNumber, _eventSequenceId);
        }
        catch (Exception ex)
        {
            _logger.AppendFailure(
                sequenceNumber,
                _eventSequenceId,
                _eventStore,
                _namespace,
                ex);
            throw;
        }
    }

    /// <inheritdoc/>
    public Task Compensate(
        EventSequenceNumber sequenceNumber,
        EventType eventType,
        IEnumerable<Causation> causation,
        IEnumerable<IdentityId> causedByChain,
        DateTimeOffset occurred,
        DateTimeOffset validFrom,
        ExpandoObject content) => throw new NotImplementedException();

    /// <inheritdoc/>
    public async Task<AppendedEvent> Redact(
        EventSequenceNumber sequenceNumber,
        RedactionReason reason,
        IEnumerable<Causation> causation,
        IEnumerable<IdentityId> causedByChain,
        DateTimeOffset occurred)
    {
        _logger.Redacting(_eventSequenceId, sequenceNumber);
        var collection = GetCollection();

        var @event = await GetEventAt(sequenceNumber);
        if (@event.Metadata.Type == GlobalEventTypes.Redaction)
        {
            _logger.RedactionAlreadyApplied(_eventSequenceId, sequenceNumber);
            return @event;
        }

        var updateModel = CreateRedactionUpdateModelFor(@event, reason, causation, causedByChain, occurred);
        await collection.UpdateOneAsync(updateModel.Filter, updateModel.Update).ConfigureAwait(false);

        return @event;
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<EventType>> Redact(
        EventSourceId eventSourceId,
        RedactionReason reason,
        IEnumerable<EventType>? eventTypes,
        IEnumerable<Causation> causation,
        IEnumerable<IdentityId> causedByChain,
        DateTimeOffset occurred)
    {
        _logger.RedactingMultiple(_eventSequenceId, eventSourceId, eventTypes ?? Enumerable.Empty<EventType>());
        var collection = GetCollection();
        var updates = new List<UpdateOneModel<Event>>();
        var affectedEventTypes = new HashSet<EventType>();

        using var cursor = await GetFromSequenceNumber(EventSequenceNumber.First, eventSourceId, eventTypes);
        while (await cursor.MoveNext())
        {
            foreach (var @event in cursor.Current)
            {
                if (@event.Metadata.Type.Id == GlobalEventTypes.Redaction)
                {
                    _logger.RedactionAlreadyApplied(_eventSequenceId, @event.Metadata.SequenceNumber);
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
        IEnumerable<EventType>? eventTypes = null,
        EventSourceId? eventSourceId = null)
    {
        _logger.GettingHeadSequenceNumber(_eventSequenceId);

        var collection = GetCollection();
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
        IEnumerable<EventType>? eventTypes = null,
        EventSourceId? eventSourceId = null)
    {
        _logger.GettingTailSequenceNumber(_eventSequenceId);

        var collection = GetCollection();
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
        IEnumerable<EventType> eventTypes)
    {
        var collection = _database.GetEventSequenceCollectionAsBsonFor(_eventSequenceId);
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
            _eventSequenceId,
            eventTypes.ToImmutableList(),
            hasTail ? new EventSequenceNumber((ulong)tail!.AsInt64) : EventSequenceNumber.Unavailable,
            hasTailForEventTypes ? new EventSequenceNumber((ulong)tailForEventTypes!.AsInt64) : EventSequenceNumber.Unavailable));
    }

    /// <inheritdoc/>
    public async Task<IImmutableDictionary<EventType, EventSequenceNumber>> GetTailSequenceNumbersForEventTypes(IEnumerable<EventType> eventTypes)
    {
        var eventTypeIds = eventTypes.Select(_ => _.Id.Value).ToArray();
        _logger.GettingTailSequenceNumbersForEventTypes(_eventSequenceId, eventTypeIds);

        var collection = _database.GetEventSequenceCollectionAsBsonFor(_eventSequenceId);

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
        EventSequenceNumber sequenceNumber,
        IEnumerable<EventType>? eventTypes = null,
        EventSourceId? eventSourceId = null)
    {
        var collection = _database.GetEventSequenceCollectionFor(_eventSequenceId);
        var filters = new List<FilterDefinition<Event>>
            {
                Builders<Event>.Filter.Gte(_ => _.SequenceNumber, sequenceNumber)
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
    public async Task<bool> HasInstanceFor(EventTypeId eventTypeId, EventSourceId eventSourceId)
    {
        var filter = Builders<Event>.Filter.And(
            Builders<Event>.Filter.Eq(_ => _.Type, eventTypeId),
            Builders<Event>.Filter.Eq(_ => _.EventSourceId, eventSourceId));

        var collection = GetCollection();
        var count = await collection.Find(filter)
                                    .CountDocumentsAsync()
                                    .ConfigureAwait(false);
        return count > 0;
    }

    /// <inheritdoc/>
    public async Task<AppendedEvent> GetEventAt(EventSequenceNumber sequenceNumber)
    {
        _logger.GettingEventAtSequenceNumber(_eventSequenceId, sequenceNumber);

        var filter = Builders<Event>.Filter.And(Builders<Event>.Filter.Eq(_ => _.SequenceNumber, sequenceNumber));

        var collection = GetCollection();
        var @event = await collection.Find(filter)
                                     .SortByDescendingSequenceNumber()
                                     .Limit(1)
                                     .SingleAsync()
                                     .ConfigureAwait(false);
        return await _converter.ToAppendedEvent(@event);
    }

    /// <inheritdoc/>
    public async Task<AppendedEvent> GetLastInstanceFor(
        EventTypeId eventTypeId,
        EventSourceId eventSourceId)
    {
        _logger.GettingLastInstanceFor(_eventSequenceId, eventTypeId, eventSourceId);

        var filter = Builders<Event>.Filter.And(
            Builders<Event>.Filter.Eq(_ => _.Type, eventTypeId),
            Builders<Event>.Filter.Eq(_ => _.EventSourceId, eventSourceId));

        var collection = GetCollection();
        var @event = await collection.Find(filter)
                                     .SortByDescendingSequenceNumber()
                                     .Limit(1)
                                     .SingleOrDefaultAsync()
                                     .ConfigureAwait(false) ?? throw new MissingEvent(_eventSequenceId, eventTypeId, eventSourceId);
        return await _converter.ToAppendedEvent(@event);
    }

    /// <inheritdoc/>
    public async Task<AppendedEvent> GetLastInstanceOfAny(
        EventSourceId eventSourceId,
        IEnumerable<EventTypeId> eventTypes)
    {
        _logger.GettingLastInstanceOfAny(_eventSequenceId, eventSourceId, eventTypes);

        var anyEventTypes = Builders<Event>.Filter.In(e => e.Type, eventTypes);

        var filter = Builders<Event>.Filter.And(
            anyEventTypes,
            Builders<Event>.Filter.Eq(_ => _.EventSourceId, eventSourceId));

        var collection = GetCollection();
        var @event = await collection.Find(filter)
                                     .SortByDescendingSequenceNumber()
                                     .Limit(1)
                                     .SingleAsync()
                                     .ConfigureAwait(false);
        return await _converter.ToAppendedEvent(@event);
    }

    /// <inheritdoc/>
    public async Task<IEventCursor> GetFromSequenceNumber(
        EventSequenceNumber sequenceNumber,
        EventSourceId? eventSourceId = null,
        IEnumerable<EventType>? eventTypes = null,
        CancellationToken cancellationToken = default)
    {
        _logger.GettingFromSequenceNumber(_eventSequenceId, sequenceNumber);

        var collection = GetCollection();
        var filters = new List<FilterDefinition<Event>>
            {
                Builders<Event>.Filter.Gte(_ => _.SequenceNumber, sequenceNumber)
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
        return new EventCursor(_converter, cursor);
    }

    /// <inheritdoc/>
    public async Task<IEventCursor> GetRange(
        EventSequenceNumber start,
        EventSequenceNumber end,
        EventSourceId? eventSourceId = default,
        IEnumerable<EventType>? eventTypes = default,
        CancellationToken cancellationToken = default)
    {
        _logger.GettingRange(_eventSequenceId, start, end);
        var collection = GetCollection();
        var filters = new List<FilterDefinition<Event>>
            {
                Builders<Event>.Filter.Gte(_ => _.SequenceNumber, start),
                Builders<Event>.Filter.Lte(_ => _.SequenceNumber, end)
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
        return new EventCursor(_converter, cursor);
    }

    IMongoCollection<Event> GetCollection() => _database.GetEventSequenceCollectionFor(_eventSequenceId);

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
