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
    /// <inheritdoc/>
    public async Task<Option<EventPublicationReceipt>> TryGetPublication(EventPublication publication)
    {
        var existing = await _collection.Find(Builders<Event>.Filter.Eq(_ => _.PublicationId, publication.Id))
            .SingleOrDefaultAsync().ConfigureAwait(false);
        if (existing is null)
        {
            return Option<EventPublicationReceipt>.None();
        }

        if (!string.Equals(existing.PublicationFingerprint, publication.Fingerprint, StringComparison.Ordinal))
        {
            throw new EventPublicationConflict();
        }

        return new EventPublicationReceipt(existing.SequenceNumber);
    }

    /// <inheritdoc/>
    public async Task<Result<EventPublicationReceipt, DuplicateEventSequenceNumber>> AppendPublication(EventPublication publication, EventToAppendToStorage @event)
    {
        if ((await TryGetPublication(publication)).TryGetValue(out var existing))
        {
            return existing;
        }

        var content = new Dictionary<string, BsonDocument>();
        foreach (var (generation, value) in @event.GenerationalContent)
        {
            var schema = await eventTypesStorage.GetFor(@event.EventType.Id, generation);
            content[generation.ToString()] = SerializeContent(value, schema.Schema);
        }

        var document = new Event(
            @event.SequenceNumber,
            @event.CorrelationId,
            @event.Causation.Select(StoredTimestamps.Normalize).ToArray(),
            @event.CausedByChain.ToArray(),
            @event.EventType.Id,
            StoredTimestamps.Normalize(@event.Occurred),
            @event.EventSourceType,
            @event.EventSourceId,
            @event.EventStreamType,
            @event.EventStreamId,
            @event.Tags.Select(_ => _.Value).ToArray(),
            content,
            @event.ContentHashes.ToDictionary(_ => _.Key.ToString(), _ => _.Value.Value),
            [],
            @event.Subject?.IsSet == true ? @event.Subject : null)
        {
            AppendedGeneration = @event.EventType.Generation.Value,
            PublicationId = publication.Id,
            PublicationFingerprint = publication.Fingerprint,
            EventSource = @event.EventSource.IsSet ? @event.EventSource : null,
            NamedTags = @event.NamedTags.Select(_ => new NamedTagDocument(_.Name.Value, _.Value)).ToArray()
        };

        try
        {
            await _collection.InsertOneAsync(document).ConfigureAwait(false);
            return new EventPublicationReceipt(@event.SequenceNumber);
        }
        catch (MongoWriteException exception) when (exception.WriteError.Category == ServerErrorCategory.DuplicateKey)
        {
            // The unique publication index may have won at a DIFFERENT sequence slot. Resolve it
            // first; a sequence-slot collision alone is not evidence of successful publication.
            if ((await TryGetPublication(publication)).TryGetValue(out var receipt))
            {
                return receipt;
            }

            // Neither a publication nor a slot collision: some other unique violation. Retrying with a
            // new slot would loop forever, so surface it.
            if (!await _collection.Find(Builders<Event>.Filter.Eq(_ => _.SequenceNumber, @event.SequenceNumber)).AnyAsync().ConfigureAwait(false))
            {
                throw;
            }

            var highest = await _collection.Find(FilterDefinition<Event>.Empty)
                .SortByDescendingSequenceNumber().Limit(1).SingleOrDefaultAsync().ConfigureAwait(false);
            return new DuplicateEventSequenceNumber(highest?.SequenceNumber.Next() ?? EventSequenceNumber.First);
        }
    }
}
