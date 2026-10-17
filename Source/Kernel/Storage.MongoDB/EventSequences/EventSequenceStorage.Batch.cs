// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Storage.EventSequences;
using Cratis.Monads;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.EventSequences;

public partial class EventSequenceStorage
{
    async Task<Result<IEnumerable<AppendedEvent>, AppendManyStorageFailure>> AppendBatch(IEnumerable<EventToAppendToStorage> events, bool persistIdentities)
    {
        EventToAppendToStorage[] eventsArray;
        try
        {
            eventsArray = events.Select(@event => @event with
            {
                Occurred = StoredTimestamps.Normalize(@event.Occurred),
                Causation = @event.Causation.Select(StoredTimestamps.Normalize).ToArray()
            }).ToArray();
        }
        catch (Exception error)
        {
            throw new EventsNotCommitted(error);
        }
        if (eventsArray.Length == 0)
        {
            return Result<IEnumerable<AppendedEvent>, AppendManyStorageFailure>.Success([]);
        }

        IClientSessionHandle session;
        try
        {
            session = await database.Client.StartSessionAsync().ConfigureAwait(false);
        }
        catch (Exception error)
        {
            throw new EventsNotCommitted(error);
        }
        using var sessionLifetime = session;
        var writeIssued = false;

        try
        {
            session.StartTransaction();
            var collection = _collection;
            var eventsToInsert = new List<Event>();
            var appendedEvents = new List<AppendedEvent>();

            foreach (var eventToAppend in eventsArray)
            {
                var generationalContent = new Dictionary<string, BsonDocument>();
                foreach (var (generation, content) in eventToAppend.GenerationalContent)
                {
                    var schema = await eventTypesStorage.GetFor(eventToAppend.EventType.Id, generation);
                    generationalContent[generation.ToString()] = SerializeContent(content, schema.Schema);
                }

                var hashesForStorage = eventToAppend.ContentHashes.ToDictionary(
                    kvp => kvp.Key.ToString(),
                    kvp => kvp.Value.Value);
                var resolvedSubject = eventToAppend.Subject?.IsSet == true
                    ? eventToAppend.Subject
                    : new Subject(eventToAppend.EventSourceId.Value);

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
                    generationalContent,
                    hashesForStorage,
                    [],
                    Subject: eventToAppend.Subject?.IsSet == true ? eventToAppend.Subject : null)
                {
                    AppendedGeneration = eventToAppend.EventType.Generation.Value,
                    PublicationId = persistIdentities ? eventToAppend.Identity?.Id : null,
                    PublicationFingerprint = persistIdentities ? eventToAppend.Identity?.Fingerprint : null,
                    NamedTags = eventToAppend.NamedTags.Select(tag => new NamedTagDocument(tag.Name.Value, tag.Value)).ToArray(),
                    EventSource = eventToAppend.EventSource.IsSet ? eventToAppend.EventSource : null
                };

                eventsToInsert.Add(@event);

                var appendedHash = eventToAppend.ContentHashes.TryGetValue(eventToAppend.EventType.Generation, out var contentHash) ? contentHash : EventHash.NotSet;
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
                        appendedHash,
                        Subject: resolvedSubject)
                    {
                        NamedTags = eventToAppend.NamedTags,
                        EventSource = eventToAppend.EventSource,
                        AppendedGeneration = eventToAppend.EventType.Generation
                    },
                    eventToAppend.GenerationalContent[eventToAppend.EventType.Generation])
                {
                    GenerationalContent = generationalContent.ToDictionary(kvp => int.Parse(kvp.Key), kvp => EventContentBson.ToJson(kvp.Value)),
                    GenerationalHashes = eventToAppend.ContentHashes.ToDictionary(_ => (int)_.Key.Value, _ => _.Value)
                });
            }

            logger.AppendingInserting(eventsToInsert.Count, eventSequenceId);
            writeIssued = true;
            await collection.InsertManyAsync(session, eventsToInsert).ConfigureAwait(false);

            await session.CommitTransactionAsync().ConfigureAwait(false);
            logger.AppendingInserted(eventsToInsert.Count, appendedEvents.Count, eventSequenceId);
            return Result<IEnumerable<AppendedEvent>, AppendManyStorageFailure>.Success(appendedEvents);
        }
        catch (MongoException error) when (persistIdentities && error.HasErrorLabel("UnknownTransactionCommitResult"))
        {
            throw;
        }
        catch (MongoException error) when (persistIdentities && IsReceiptUniqueViolation(error))
        {
            await AbortReceiptTransaction(session).ConfigureAwait(false);
            if (await ClassifyIdentityCollision(eventsArray) is { } collision)
            {
                return Result<IEnumerable<AppendedEvent>, AppendManyStorageFailure>.Failed(collision);
            }
            throw;
        }
        catch (MongoWriteException writeException) when (writeException.WriteError.Category == ServerErrorCategory.DuplicateKey)
        {
            return Result<IEnumerable<AppendedEvent>, AppendManyStorageFailure>.Failed(await AbortAndResolveNextAvailableSequenceNumber(session).ConfigureAwait(false));
        }
        catch (MongoBulkWriteException<Event> bulkWriteException) when (bulkWriteException.WriteErrors.Any(writeError => writeError.Category == ServerErrorCategory.DuplicateKey))
        {
            return Result<IEnumerable<AppendedEvent>, AppendManyStorageFailure>.Failed(await AbortAndResolveNextAvailableSequenceNumber(session).ConfigureAwait(false));
        }
        catch (MongoException error) when (error.HasErrorLabel("UnknownTransactionCommitResult"))
        {
            throw;
        }
        catch (Exception error)
        {
            await AbortReceiptTransaction(session).ConfigureAwait(false);
            throw persistIdentities ? ClassifyIdentityAppendFailure(error, writeIssued) : ClassifyAppendFailure(error, writeIssued);
        }
    }
}
