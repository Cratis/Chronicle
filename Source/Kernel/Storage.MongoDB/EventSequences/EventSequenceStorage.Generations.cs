// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Storage.EventSequences;
using Cratis.Strings;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.EventSequences;

public partial class EventSequenceStorage
{
    /// <inheritdoc/>
    public async Task<StoredEventGenerations?> GetStoredGenerations(EventSequenceNumber sequenceNumber)
    {
        var stored = await _collection.Find(_ => _.SequenceNumber == sequenceNumber).FirstOrDefaultAsync().ConfigureAwait(false);
        if (stored is null)
        {
            return null;
        }

        return new(
            sequenceNumber,
            stored.Type,
            stored.EventSourceId,
            stored.Subject ?? Subject.NotSet,
            stored.AppendedGeneration is { } appended ? new EventTypeGeneration(appended) : null,
            stored.Content.ToDictionary(_ => new EventTypeGeneration(uint.Parse(_.Key)), _ => EventContentBson.ToJson(_.Value)),
            stored.Revisions.Count(),
            string.Empty);
    }

    /// <inheritdoc/>
    public async Task<bool> TryAddGenerations(StoredEventGenerations observed, IEnumerable<GenerationToAdd> generations)
    {
        var additions = generations.ToArray();
        if (observed.EventTypeId == GlobalEventTypes.Redaction || additions.Length == 0 ||
            additions.Select(_ => _.Generation).Distinct().Count() != additions.Length)
        {
            return false;
        }

        var builder = Builders<Event>.Filter;
        var filter = builder.Eq(_ => _.SequenceNumber, observed.SequenceNumber) &
            builder.Eq(_ => _.Type, observed.EventTypeId) & builder.Size(_ => _.Revisions, observed.RevisionCount);
        var contentField = nameof(Event.Content).ToCamelCase();
        foreach (var (generation, content) in observed.Content)
        {
            var schema = await eventTypesStorage.GetFor(observed.EventTypeId, generation);
            filter &= builder.Eq($"{contentField}.{generation.Value}", EventContentBson.FromJson(content, schema.Schema));
        }

        var updates = new List<UpdateDefinition<Event>>();
        foreach (var addition in additions)
        {
            var generation = addition.Generation.Value.ToString();
            filter &= builder.Exists($"{contentField}.{generation}", false);
            var schema = await eventTypesStorage.GetFor(observed.EventTypeId, addition.Generation);
            var provenance = new DerivedGeneration(addition.Provenance.Source.Value, addition.Provenance.SourceIsAppended, addition.Provenance.MigrationsVersion.Value);
            updates.Add(Builders<Event>.Update
                .Set($"{contentField}.{generation}", SerializeContent(addition.Content, schema.Schema))
                .Set($"{nameof(Event.ContentHashes).ToCamelCase()}.{generation}", addition.Hash.Value)
                .Set($"{nameof(Event.DerivedGenerations).ToCamelCase()}.{generation}", provenance.ToBsonDocument()));
        }

        var result = await _collection.UpdateOneAsync(filter, Builders<Event>.Update.Combine(updates)).ConfigureAwait(false);

        return result.ModifiedCount == 1;
    }
}
