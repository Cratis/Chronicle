// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Dynamic;
using System.Text.Json;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Auditing;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Identities;
using Cratis.Chronicle.Storage.EventSequences;
using Cratis.Chronicle.Storage.EventTypes;
using Cratis.Chronicle.Storage.Identities;
using Cratis.Monads;
using Cratis.Strings;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.EventSequences;

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
/// <param name="identityStorage"><see cref="IIdentityStorage"/>.</param>
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
    IIdentityStorage identityStorage,
    Json.IExpandoObjectConverter expandoObjectConverter,
    JsonSerializerOptions jsonSerializerOptions,
    ILogger<EventSequenceStorage> logger) : IEventSequenceStorage
{
    readonly IMongoCollection<Event> _collection = database.GetEventSequenceCollectionFor(eventSequenceId);

    /// <inheritdoc/>
    public async Task<Chronicle.Storage.EventSequences.EventSequenceState> GetState()
    {
        var collection = database.GetCollection<EventSequenceState>(WellKnownCollectionNames.EventSequences);
        var filter = Builders<EventSequenceState>.Filter.Eq(new StringFieldDefinition<EventSequenceState, string>("_id"), eventSequenceId);
        using var cursor = await collection.FindAsync(filter).ConfigureAwait(false);
        var state = await cursor.FirstOrDefaultAsync();
        return state?.ToChronicle() ?? new Chronicle.Storage.EventSequences.EventSequenceState();
    }

    /// <inheritdoc/>
    public async Task SaveState(Chronicle.Storage.EventSequences.EventSequenceState state)
    {
        var collection = database.GetCollection<EventSequenceState>(WellKnownCollectionNames.EventSequences);
        var filter = Builders<EventSequenceState>.Filter.Eq(new StringFieldDefinition<EventSequenceState, string>("_id"), eventSequenceId);

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
        var collection = _collection;
        return await collection.CountDocumentsAsync(filter).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<Result<AppendedEvent, DuplicateEventSequenceNumber>> Append(
        EventSequenceNumber sequenceNumber,
        EventSourceType eventSourceType,
        EventSourceId eventSourceId,
        EventStreamType eventStreamType,
        EventStreamId eventStreamId,
        EventType eventType,
        CorrelationId correlationId,
        IEnumerable<Causation> causation,
        IEnumerable<IdentityId> causedByChain,
        IEnumerable<Cratis.Chronicle.Concepts.Events.Tag> tags,
        DateTimeOffset occurred,
        ExpandoObject content,
        EventHash hash)
    {
        try
        {
            var schema = await eventTypesStorage.GetFor(eventType.Id, eventType.Generation);
            var jsonObject = expandoObjectConverter.ToJsonObject(content, schema.Schema);
            var document = BsonDocument.Parse(JsonSerializer.Serialize(jsonObject, jsonSerializerOptions));
            var @event = new Event(
                sequenceNumber,
                correlationId,
                causation,
                causedByChain,
                eventType.Id,
                occurred,
                eventSourceType,
                eventSourceId,
                eventStreamType,
                eventStreamId,
                tags.Select(_ => _.Value),
                new Dictionary<string, BsonDocument>
                {
                    { eventType.Generation.ToString(), document }
                },
                new Dictionary<string, string>
                {
                    { eventType.Generation.ToString(), hash.Value }
                },
                []);
            var collection = _collection;
            await collection.InsertOneAsync(@event).ConfigureAwait(false);

            return Result<AppendedEvent, DuplicateEventSequenceNumber>.Success(new AppendedEvent(
                new(
                    eventType,
                    eventSourceType,
                    eventSourceId,
                    eventStreamType,
                    eventStreamId,
                    sequenceNumber,
                    occurred,
                    eventStore,
                    @namespace,
                    correlationId,
                    causation,
                    await identityStorage.GetFor(causedByChain),
                    tags,
                    hash),
                content));
        }
        catch (MongoWriteException writeException) when (writeException.WriteError.Category == ServerErrorCategory.DuplicateKey)
        {
            // Get the highest sequence number in the collection and add 1 to get the next available
            var highest = await _collection.Find(FilterDefinition<Event>.Empty)
                                          .SortByDescendingSequenceNumber()
                                          .Limit(1)
                                          .SingleOrDefaultAsync()
                                          .ConfigureAwait(false);
            var nextAvailableSequenceNumber = highest?.SequenceNumber.Next() ?? EventSequenceNumber.First;
            return new DuplicateEventSequenceNumber(nextAvailableSequenceNumber);
        }
    }

    /// <inheritdoc/>
    public async Task<Result<IEnumerable<AppendedEvent>, DuplicateEventSequenceNumber>> AppendMany(IEnumerable<EventToAppendToStorage> events)
    {
        var eventsArray = events.ToArray();
        if (eventsArray.Length == 0)
        {
            return Result<IEnumerable<AppendedEvent>, DuplicateEventSequenceNumber>.Success([]);
        }

        var client = database.Client;
        using var session = await client.StartSessionAsync().ConfigureAwait(false);

        try
        {
            session.StartTransaction();
            var collection = _collection;
            var eventsToInsert = new List<Event>();
            var appendedEvents = new List<AppendedEvent>();

            foreach (var eventToAppend in eventsArray)
            {
                var schema = await eventTypesStorage.GetFor(eventToAppend.EventType.Id, eventToAppend.EventType.Generation);
                var jsonObject = expandoObjectConverter.ToJsonObject(eventToAppend.Content, schema.Schema);
                var document = BsonDocument.Parse(JsonSerializer.Serialize(jsonObject, jsonSerializerOptions));
                var @event = new Event(
                    eventToAppend.SequenceNumber,
                    eventToAppend.CorrelationId,
                    eventToAppend.Causation,
                    eventToAppend.CausedByChain,
                    eventToAppend.EventType.Id,
                    eventToAppend.Occurred,
                    eventToAppend.EventSourceType,
                    eventToAppend.EventSourceId,
                    eventToAppend.EventStreamType,
                    eventToAppend.EventStreamId,
                    eventToAppend.Tags.Select(_ => _.Value),
                    new Dictionary<string, BsonDocument>
                    {
                        { eventToAppend.EventType.Generation.ToString(), document }
                    },
                    new Dictionary<string, string>
                    {
                        { eventToAppend.EventType.Generation.ToString(), eventToAppend.Hash.Value }
                    },
                    []);

                eventsToInsert.Add(@event);

                appendedEvents.Add(new AppendedEvent(
                    new(
                        eventToAppend.EventType,
                        eventToAppend.EventSourceType,
                        eventToAppend.EventSourceId,
                        eventToAppend.EventStreamType,
                        eventToAppend.EventStreamId,
                        eventToAppend.SequenceNumber,
                        eventToAppend.Occurred,
                        eventStore,
                        @namespace,
                        eventToAppend.CorrelationId,
                        eventToAppend.Causation,
                        await identityStorage.GetFor(eventToAppend.CausedByChain),
                        eventToAppend.Tags,
                        eventToAppend.Hash),
                    eventToAppend.Content));
            }

            logger.AppendingInserting(eventsToInsert.Count, eventSequenceId);
            await collection.InsertManyAsync(session, eventsToInsert).ConfigureAwait(false);

            await session.CommitTransactionAsync().ConfigureAwait(false);
            logger.AppendingInserted(eventsToInsert.Count, appendedEvents.Count, eventSequenceId);
            return Result<IEnumerable<AppendedEvent>, DuplicateEventSequenceNumber>.Success(appendedEvents);
        }
        catch (MongoWriteException writeException) when (writeException.WriteError.Category == ServerErrorCategory.DuplicateKey)
        {
            await session.AbortTransactionAsync().ConfigureAwait(false);
            var highest = await _collection.Find(FilterDefinition<Event>.Empty)
                                          .SortByDescendingSequenceNumber()
                                          .Limit(1)
                                          .SingleOrDefaultAsync()
                                          .ConfigureAwait(false);
            var nextAvailableSequenceNumber = highest?.SequenceNumber.Next() ?? EventSequenceNumber.First;
            return new DuplicateEventSequenceNumber(nextAvailableSequenceNumber);
        }
        catch
        {
            await session.AbortTransactionAsync().ConfigureAwait(false);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task Compensate(
        EventSequenceNumber sequenceNumber,
        EventType eventType,
        CorrelationId correlationId,
        IEnumerable<Causation> causation,
        IEnumerable<IdentityId> causedByChain,
        DateTimeOffset occurred,
        ExpandoObject content)
    {
        logger.Compensating(eventSequenceId, sequenceNumber);
        var collection = _collection;

        var @event = await GetEventAt(sequenceNumber);

        var schema = await eventTypesStorage.GetFor(eventType.Id, eventType.Generation);
        var jsonObject = expandoObjectConverter.ToJsonObject(content, schema.Schema);
        var document = BsonDocument.Parse(JsonSerializer.Serialize(jsonObject, jsonSerializerOptions));

        var compensation = new EventCompensation(
            eventType.Generation,
            correlationId,
            causation,
            causedByChain.First(),
            occurred,
            new Dictionary<string, BsonDocument>
            {
                { eventType.Generation.ToString(), document }
            });

        var filter = Builders<Event>.Filter.Eq(e => e.SequenceNumber, sequenceNumber);
        var update = Builders<Event>.Update.Push(e => e.Compensations, compensation);

        await collection.UpdateOneAsync(filter, update).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<AppendedEvent> Redact(
        EventSequenceNumber sequenceNumber,
        RedactionReason reason,
        CorrelationId correlationId,
        IEnumerable<Causation> causation,
        IEnumerable<IdentityId> causedByChain,
        DateTimeOffset occurred)
    {
        logger.Redacting(eventSequenceId, sequenceNumber);
        var collection = _collection;

        var @event = await GetEventAt(sequenceNumber);
        if (@event.Context.EventType == GlobalEventTypes.Redaction)
        {
            logger.RedactionAlreadyApplied(eventSequenceId, sequenceNumber);
            return @event;
        }

        var updateModel = CreateRedactionUpdateModelFor(@event, reason, correlationId, causation, causedByChain, occurred);
        await collection.UpdateOneAsync(updateModel.Filter, updateModel.Update).ConfigureAwait(false);

        return @event;
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<EventType>> Redact(
        EventSourceId eventSourceId,
        RedactionReason reason,
        IEnumerable<EventType>? eventTypes,
        CorrelationId correlationId,
        IEnumerable<Causation> causation,
        IEnumerable<IdentityId> causedByChain,
        DateTimeOffset occurred)
    {
        logger.RedactingMultiple(eventSequenceId, eventSourceId, eventTypes ?? []);
        var collection = _collection;
        var updates = new List<UpdateOneModel<Event>>();
        var affectedEventTypes = new HashSet<EventType>();

        using var cursor = await GetFromSequenceNumber(EventSequenceNumber.First, eventSourceId, eventTypes: eventTypes);
        while (await cursor.MoveNext())
        {
            foreach (var @event in cursor.Current)
            {
                if (@event.Context.EventType.Id == GlobalEventTypes.Redaction)
                {
                    logger.RedactionAlreadyApplied(eventSequenceId, @event.Context.SequenceNumber);
                    continue;
                }

                updates.Add(CreateRedactionUpdateModelFor(@event, reason, correlationId, causation, causedByChain, occurred));
                affectedEventTypes.Add(@event.Context.EventType);
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

        var collection = _collection;
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
                                      .SortByAscendingSequenceNumber()
                                      .Limit(1)
                                      .SingleOrDefaultAsync()
                                      .ConfigureAwait(false);
        return highest?.SequenceNumber ?? EventSequenceNumber.Unavailable;
    }

    /// <inheritdoc/>
    public async Task<EventSequenceNumber> GetTailSequenceNumber(
        IEnumerable<EventType>? eventTypes = null,
        EventSourceId? eventSourceId = null,
        EventSourceType? eventSourceType = null,
        EventStreamId? eventStreamId = null,
        EventStreamType? eventStreamType = null)
    {
        logger.GettingTailSequenceNumber(eventSequenceId);

        var collection = _collection;
        var filters = new List<FilterDefinition<Event>>();

        if (eventTypes?.Any() == true)
        {
            filters.Add(Builders<Event>.Filter.In(e => e.Type, eventTypes.Select(_ => _.Id).ToArray()));
        }

        if (eventSourceId?.IsSpecified == true)
        {
            filters.Add(Builders<Event>.Filter.Eq(e => e.EventSourceId, eventSourceId));
        }

        if (eventSourceType?.IsDefaultOrUnspecified == true)
        {
            filters.Add(Builders<Event>.Filter.Eq(e => e.EventSourceType, eventSourceType));
        }

        if (eventStreamType?.IsAll == false)
        {
            filters.Add(Builders<Event>.Filter.Eq(e => e.EventStreamType, eventStreamType));
        }

        if (eventStreamId?.IsDefault == false)
        {
            filters.Add(Builders<Event>.Filter.Eq(e => e.EventStreamId, eventStreamId));
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
        var tailPipeline = PipelineDefinition<BsonDocument, BsonDocument>.Create([sort, limit, tailProjection]);
        var tailFacet = AggregateFacet.Create("tail", tailPipeline);

        var filter = Builders<BsonDocument>.Filter.In(nameof(Event.Type).ToCamelCase(), eventTypes.Select(_ => _.Id));
        var tailForEventTypesMatch = PipelineStageDefinitionBuilder.Match(filter);
        var tailForEventTypesProjection = PipelineStageDefinitionBuilder.Project<BsonDocument>(/*lang=json*/ $"{{ '{nameof(TailEventSequenceNumbers.TailForEventTypes)}': '$_id'}}");
        var tailForEventTypesPipeline = PipelineDefinition<BsonDocument, BsonDocument>.Create([tailForEventTypesMatch, sort, limit, tailForEventTypesProjection]);
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

        var eventTypesFilter = Builders<BsonDocument>.Filter.In(new StringFieldDefinition<BsonDocument, string>(nameof(Event.Type).ToCamelCase()), eventTypeIds);
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
            var eventType = eventTypes.FirstOrDefault(_ => _.Id == (EventTypeId)item["_id"].AsString);
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
                                      .SortByDescendingSequenceNumber()
                                      .Limit(1)
                                      .SingleOrDefaultAsync()
                                      .ConfigureAwait(false);
        return highest?.SequenceNumber ?? EventSequenceNumber.Unavailable;
    }

    /// <inheritdoc/>
    public async Task<bool> HasEventsFor(EventSourceId eventSourceId)
    {
        var filter = Builders<Event>.Filter.Eq(_ => _.EventSourceId, eventSourceId);
        var collection = _collection;
        var count = await collection.Find(filter)
                                    .CountDocumentsAsync()
                                    .ConfigureAwait(false);
        return count > 0;
    }

    /// <inheritdoc/>
    public async Task<Catch<Option<AppendedEvent>>> TryGetLastEventBefore(EventTypeId eventTypeId, EventSourceId eventSourceId, EventSequenceNumber currentSequenceNumber)
    {
        try
        {
            var filter = Builders<Event>.Filter.And(
                Builders<Event>.Filter.Eq(_ => _.Type, eventTypeId),
                Builders<Event>.Filter.Eq(_ => _.EventSourceId, eventSourceId),
                Builders<Event>.Filter.Lt(_ => _.SequenceNumber, currentSequenceNumber));

            var @event = await _collection.Find(filter)
                .SortByDescendingSequenceNumber()
                .Limit(1)
                .FirstOrDefaultAsync()
                .ConfigureAwait(false);

            return @event != null
                ? await converter.ToAppendedEvent(@event)
                : Option<AppendedEvent>.None();
        }
        catch (Exception ex)
        {
            return ex;
        }
    }

    /// <inheritdoc/>
    public async Task<AppendedEvent> GetEventAt(EventSequenceNumber sequenceNumber)
    {
        logger.GettingEventAtSequenceNumber(eventSequenceId, sequenceNumber);

        var filter = Builders<Event>.Filter.And(Builders<Event>.Filter.Eq(_ => _.SequenceNumber, sequenceNumber));

        var collection = _collection;
        var @event = await collection.Find(filter)
                                     .SortByDescendingSequenceNumber()
                                     .Limit(1)
                                     .SingleAsync()
                                     .ConfigureAwait(false);
        return await converter.ToAppendedEvent(@event);
    }

    /// <inheritdoc/>
    public async Task<Option<AppendedEvent>> TryGetLastInstanceOfAny(
        EventSourceId eventSourceId,
        IEnumerable<EventTypeId> eventTypes)
    {
        logger.GettingLastInstanceOfAny(eventSequenceId, eventSourceId, eventTypes);
        var anyEventTypes = Builders<Event>.Filter.In(e => e.Type, eventTypes);
        var filter = Builders<Event>.Filter.And(
            anyEventTypes,
            Builders<Event>.Filter.Eq(_ => _.EventSourceId, eventSourceId));

        var collection = _collection;
        var @event = await collection.Find(filter)
                                    .SortByDescendingSequenceNumber()
                                    .Limit(1)
                                    .SingleOrDefaultAsync()
                                    .ConfigureAwait(false);
        return @event is null
            ? Option<AppendedEvent>.None()
            : await converter.ToAppendedEvent(@event);
    }

    /// <inheritdoc/>
    public async Task<IEventCursor> GetFromSequenceNumber(
        EventSequenceNumber sequenceNumber,
        EventSourceId? eventSourceId = null,
        EventStreamType? eventStreamType = default,
        EventStreamId? eventStreamId = default,
        IEnumerable<EventType>? eventTypes = null,
        CancellationToken cancellationToken = default)
    {
        logger.GettingFromSequenceNumber(eventSequenceId, sequenceNumber);

        var collection = _collection;
        var filters = new List<FilterDefinition<Event>>
            {
                Builders<Event>.Filter.Gte(_ => _.SequenceNumber, sequenceNumber)
            };

        if (eventSourceId?.IsSpecified == true)
        {
            filters.Add(Builders<Event>.Filter.Eq(e => e.EventSourceId, eventSourceId));
        }

        if (eventStreamType?.IsAll == false)
        {
            filters.Add(Builders<Event>.Filter.Eq(e => e.EventStreamType, eventStreamType));
        }

        if (eventStreamId?.IsDefault == false)
        {
            filters.Add(Builders<Event>.Filter.Eq(e => e.EventStreamId, eventStreamId));
        }

        if (eventTypes?.Any() == true)
        {
            filters.Add(Builders<Event>.Filter.In(e => e.Type, eventTypes.Select(_ => _.Id).ToArray()));
        }

        var filter = Builders<Event>.Filter.And([.. filters]);
        var cursor = await collection.Find(filter)
                                     .SortByAscendingSequenceNumber()
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
        var collection = _collection;
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
                                     .SortByAscendingSequenceNumber()
                                     .ToCursorAsync(cancellationToken)
                                     .ConfigureAwait(false);
        return new EventCursor(converter, cursor);
    }

    /// <inheritdoc/>
    public async Task<IEventCursor> GetEventsWithLimit(
        EventSequenceNumber start,
        int limit,
        EventSourceId? eventSourceId = default,
        EventStreamType? eventStreamType = default,
        EventStreamId? eventStreamId = default,
        IEnumerable<EventType>? eventTypes = default,
        CancellationToken cancellationToken = default)
    {
        logger.GettingRange(eventSequenceId, start, EventSequenceNumber.Unavailable);

        var collection = _collection;
        var filters = new List<FilterDefinition<Event>>
            {
                Builders<Event>.Filter.Gte(_ => _.SequenceNumber, start)
            };

        if (eventSourceId?.IsSpecified == true)
        {
            filters.Add(Builders<Event>.Filter.Eq(e => e.EventSourceId, eventSourceId));
        }

        if (eventStreamType?.IsAll == false)
        {
            filters.Add(Builders<Event>.Filter.Eq(e => e.EventStreamType, eventStreamType));
        }

        if (eventStreamId?.IsDefault == false)
        {
            filters.Add(Builders<Event>.Filter.Eq(e => e.EventStreamId, eventStreamId));
        }

        if (eventTypes?.Any() == true)
        {
            filters.Add(Builders<Event>.Filter.In(e => e.Type, eventTypes.Select(_ => _.Id).ToArray()));
        }

        var filter = Builders<Event>.Filter.And([.. filters]);

        var cursor = await collection.Find(filter)
                                     .SortByAscendingSequenceNumber()
                                     .Limit(limit)
                                     .ToCursorAsync(cancellationToken)
                                     .ConfigureAwait(false);
        return new EventCursor(converter, cursor);
    }

    UpdateOneModel<Event> CreateRedactionUpdateModelFor(
        AppendedEvent @event,
        RedactionReason reason,
        CorrelationId correlationId,
        IEnumerable<Causation> causation,
        IEnumerable<IdentityId> causedById,
        DateTimeOffset occurred)
    {
        var content = new RedactionEventContent(
            reason,
            @event.Context.EventType.Id,
            occurred,
            correlationId,
            causation,
            causedById);

        var document = BsonDocument.Parse(JsonSerializer.Serialize(content, jsonSerializerOptions));
        var generationalContent = new Dictionary<string, BsonDocument>
                {
                        { EventTypeGeneration.First.ToString(), document }
                };

        return new UpdateOneModel<Event>(
            Builders<Event>.Filter.Eq(e => e.SequenceNumber, @event.Context.SequenceNumber),
            Builders<Event>.Update
                .Set(e => e.Type, GlobalEventTypes.Redaction)
                .Set(e => e.Content, generationalContent));
    }
}
