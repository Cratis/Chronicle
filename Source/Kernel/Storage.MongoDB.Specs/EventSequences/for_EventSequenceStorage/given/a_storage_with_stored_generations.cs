// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Identities;
using Cratis.Chronicle.Storage.EventSequences;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.EventSequences.for_EventSequenceStorage.given;

public abstract class a_storage_with_stored_generations(ReplicaSetMongoDBFixture fixture) : a_storage_for_appended_generation(fixture)
{
    protected StoredEventGenerations _observed;
    protected static readonly EventTypeMigrationsVersion Version = new("migration-version");

    async Task Establish()
    {
        (await _storage.AppendMany([GenerationalEvent(0, 1)])).IsSuccess.ShouldBeTrue();
        _observed = (await _storage.GetStoredGenerations(0))!;
    }

    protected static GenerationToAdd Target(uint generation = 3, uint source = 1, bool sourceIsAppended = true)
    {
        dynamic content = new ExpandoObject();
        content.value = "added";
        return new(new(generation), (ExpandoObject)content, "added-hash", new(new(source), sourceIsAppended, Version));
    }

    protected Task Revise() => _storage.Revise(0, _eventType, CorrelationId.NotSet, [], [IdentityId.NotSet], DateTimeOffset.UtcNow, new ExpandoObject(), "revision-hash");

    protected async Task<string> Fingerprint()
    {
        await Task.CompletedTask;
        return (await Stored(0)).ToBsonDocument().ToJson();
    }

    protected async Task<GenerationProvenance> Provenance(EventTypeGeneration generation)
    {
        await Task.CompletedTask;
        var value = (await Stored(0)).DerivedGenerations![generation.Value.ToString()];
        return new(new(value.SourceGeneration), value.SourceIsAppended, new(value.MigrationsVersion));
    }

    protected async Task ClearAppendedGeneration()
    {
        await _collection.UpdateOneAsync(_ => _.SequenceNumber == 0, Builders<Event>.Update.Unset(_ => _.AppendedGeneration));
    }

    protected async Task<string> Hash(EventTypeGeneration generation)
    {
        await Task.CompletedTask;
        return (await Stored(0)).ContentHashes[generation.Value.ToString()];
    }
}
