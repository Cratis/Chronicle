// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Dynamic;
using System.Text.Json;
using Cratis.Auditing;
using Cratis.Events;
using Cratis.EventSequences;
using Cratis.Identities;
using Cratis.Kernel.EventSequences;
using Cratis.Kernel.Storage.EventSequences;
using Cratis.Kernel.Storage.EventTypes;
using Cratis.Strings;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Kernel.Storage.MongoDB.EventSequences;

#pragma warning disable CA1849, MA0042 // MongoDB breaks the Orleans task model internally, so it won't return to the task scheduler

/// <summary>
/// Represents an implementation of <see cref="IEventSequenceStorage"/> for MongoDB.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="EventSequenceStorage"/> class.
/// </remarks>
/// <param name="eventStore"><see cref="EventStoreName"/> the storage is for.</param>
/// <param name="namespace"><see cref="EventStoreNamespaceName"/> the storage is for.</param>
/// <param name="eventSequenceId">The <see cref="EventSequenceId"/> the storage represent.</param>
/// <param name="database">Provider for <see cref="IEventStoreNamespaceDatabase"/> to use.</param>
/// <param name="converter"><see cref="IEventConverter"/> to convert event types.</param>
/// <param name="eventTypesStorage">The <see cref="IEventTypesStorage"/> for working with the schema types.</param>
/// <param name="expandoObjectConverter"><see cref="IExpandoObjectConverter"/> for converting between expando object and json objects.</param>
/// <param name="jsonSerializerOptions">The global <see cref="JsonSerializerOptions"/>.</param>
/// <param name="logger"><see cref="ILogger"/> for logging.</param>
public class EventSequenceStorage(
    EventStoreName eventStore,
    EventStoreNamespaceName @namespace,
    EventSequenceId eventSequenceId,
    IEventStoreNamespaceDatabase database,
    IEventConverter converter,
    IEventTypesStorage eventTypesStorage,
    Json.IExpandoObjectConverter expandoObjectConverter,
    JsonSerializerOptions jsonSerializerOptions,
    ILogger<EventSequenceStorage> logger) : IEventSequenceStorage
{
    /// <inheritdoc/>
    public async Task<Kernel.Storage.EventSequences.EventSequenceState> GetState()
    {
        var collection = database.GetCollection<EventSequenceState>(WellKnownCollectionNames.EventSequences);
        var filter = Builders<EventSequenceState>.Filter.Eq(new StringFieldDefinition<EventSequenceState, Guid>("_id"), eventSequenceId);
        var cursor = await collection.FindAsync(filter).ConfigureAwait(false);
        var state = await cursor.FirstOrDefaultAsync();
        return state?.ToKernel() ?? new Kernel.Storage.EventSequences.EventSequenceState();
    }

    /// <inheritdoc/>
    public async Task SaveState(Kernel.Storage.EventSequences.EventSequenceState state)
    {
        var collection = database.GetCollection<EventSequenceState>(WellKnownCollectionNames.EventSequences);
        var filter = Builders<EventSequenceState>.Filter.Eq(new StringFieldDefinition<EventSequenceState, Guid>("_id"), eventSequenceId);

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

        var filter = Builders<Event>.Filter.And([.. filters]);
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
            var schema = await eventTypesStorage.GetFor(eventType.Id, eventType.Generation);
            var jsonObject = expandoObjectConverter.ToJsonObject(content, schema.Schema);
            var document = BsonDocument.Parse(JsonSerializer.Serialize(jsonObject, jsonSerializerOptions));
            logger.Appending(
                sequenceNumber,
                eventSequenceId,
                eventStore,
                @namespace);
            var @event = new Event(
                sequenceNumber,
                CorrelationId.New(), // TODO: Fix this when we have a proper correlation id
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
            logger.DuplicateEventSequenceNumber(
                sequenceNumber,
                eventSequenceId,
                eventStore,
                @namespace);

            throw new DuplicateEventSequenceNumber(sequenceNumber, eventSequenceId);
        }
        catch (Exception ex)
        {
            logger.AppendFailure(
                sequenceNumber,
                eventSequenceId,
                eventStore,
                @namespace,
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
        logger.Redacting(eventSequenceId, sequenceNumber);
        var collection = GetCollection();

        var @event = await GetEventAt(sequenceNumber);
        if (@event.Metadata.Type == GlobalEventTypes.Redaction)
        {
            logger.RedactionAlreadyApplied(eventSequenceId, sequenceNumber);
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
        logger.RedactingMultiple(eventSequenceId, eventSourceId, eventTypes ?? Enumerable.Empty<EventType>());
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
                    logger.RedactionAlreadyApplied(eventSequenceId, @event.Metadata.SequenceNumber);
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
        logger.GettingHeadSequenceNumber(eventSequenceId);

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

        var filter = Builders<Event>.Filter.And([.. filters]);

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
        logger.GettingTailSequenceNumber(eventSequenceId);

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

        var filter = Builders<Event>.Filter.And([.. filters]);
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
        var collection = database.GetEventSequenceCollectionAsBsonFor(eventSequenceId);
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
    public async Task<IImmutableDictionary<EventType, EventSequenceNumber>> GetTailSequenceNumbersForEventTypes(IEnumerable<EventType> eventTypes)
    {
        var eventTypeIds = eventTypes.Select(_ => _.Id.Value).ToArray();
        logger.GettingTailSequenceNumbersForEventTypes(eventSequenceId, eventTypeIds);

        var collection = database.GetEventSequenceCollectionAsBsonFor(eventSequenceId);

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
        var collection = database.GetEventSequenceCollectionFor(eventSequenceId);
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

        var filter = Builders<Event>.Filter.And([.. filters]);
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
        logger.GettingEventAtSequenceNumber(eventSequenceId, sequenceNumber);

        var filter = Builders<Event>.Filter.And(Builders<Event>.Filter.Eq(_ => _.SequenceNumber, sequenceNumber));

        var collection = GetCollection();
        var @event = await collection.Find(filter)
                                     .SortByDescendingSequenceNumber()
                                     .Limit(1)
                                     .SingleAsync()
                                     .ConfigureAwait(false);
        return await converter.ToAppendedEvent(@event);
    }

    /// <inheritdoc/>
    public async Task<AppendedEvent> GetLastInstanceFor(
        EventTypeId eventTypeId,
        EventSourceId eventSourceId)
    {
        logger.GettingLastInstanceFor(eventSequenceId, eventTypeId, eventSourceId);

        var filter = Builders<Event>.Filter.And(
            Builders<Event>.Filter.Eq(_ => _.Type, eventTypeId),
            Builders<Event>.Filter.Eq(_ => _.EventSourceId, eventSourceId));

        var collection = GetCollection();
        var @event = await collection.Find(filter)
                                     .SortByDescendingSequenceNumber()
                                     .Limit(1)
                                     .SingleOrDefaultAsync()
                                     .ConfigureAwait(false) ?? throw new MissingEvent(eventSequenceId, eventTypeId, eventSourceId);
        return await converter.ToAppendedEvent(@event);
    }

    /// <inheritdoc/>
    public async Task<AppendedEvent> GetLastInstanceOfAny(
        EventSourceId eventSourceId,
        IEnumerable<EventTypeId> eventTypes)
    {
        logger.GettingLastInstanceOfAny(eventSequenceId, eventSourceId, eventTypes);

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
        return await converter.ToAppendedEvent(@event);
    }

    /// <inheritdoc/>
    public async Task<IEventCursor> GetFromSequenceNumber(
        EventSequenceNumber sequenceNumber,
        EventSourceId? eventSourceId = null,
        IEnumerable<EventType>? eventTypes = null,
        CancellationToken cancellationToken = default)
    {
        logger.GettingFromSequenceNumber(eventSequenceId, sequenceNumber);

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

        var filter = Builders<Event>.Filter.And([.. filters]);
        var cursor = await collection.Find(filter)
                                     .SortBySequenceNumber()
                                     .ToCursorAsync(cancellationToken)
                                     .ConfigureAwait(false);
        return new EventCursor(converter, cursor);
    }

    /// <inheritdoc/>
    public async Task<IEventCursor> GetRange(
        EventSequenceNumber start,
        EventSequenceNumber end,
        EventSourceId? eventSourceId = default,
        IEnumerable<EventType>? eventTypes = default,
        CancellationToken cancellationToken = default)
    {
        logger.GettingRange(eventSequenceId, start, end);
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

        var filter = Builders<Event>.Filter.And([.. filters]);

        var cursor = await collection.Find(filter)
                                     .SortBySequenceNumber()
                                     .ToCursorAsync(cancellationToken)
                                     .ConfigureAwait(false);
        return new EventCursor(converter, cursor);
    }

    IMongoCollection<Event> GetCollection() => database.GetEventSequenceCollectionFor(eventSequenceId);

    UpdateOneModel<Event> CreateRedactionUpdateModelFor(
        AppendedEvent @event,
        RedactionReason reason,
        IEnumerable<Causation> causation,
        IEnumerable<IdentityId> causedById,
        DateTimeOffset occurred)
    {
        var content = new RedactionEventContent(
            reason,
            @event.Metadata.Type.Id,
            occurred,
            CorrelationId.New(), // TODO: Fix this when we have a proper correlation id
            causation,
            causedById);

        var document = BsonDocument.Parse(JsonSerializer.Serialize(content, jsonSerializerOptions));
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
