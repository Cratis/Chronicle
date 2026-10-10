// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Text.Json;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Storage.EventSequences;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences.for_EventSequenceStorage.given;

public class a_storage_with_stored_generations : a_storage_for_appended_generation
{
    protected StoredEventGenerations _observed;
    protected static readonly EventTypeMigrationsVersion Version = new("migration-version");

    async Task Establish()
    {
        (await _storage.AppendMany([EventAt(0, 1)])).IsSuccess.ShouldBeTrue();
        _observed = (await _storage.GetStoredGenerations(0))!;
    }

    protected static GenerationToAdd Target(uint generation = 3, uint source = 1, bool sourceIsAppended = true)
    {
        dynamic content = new ExpandoObject();
        content.value = "added";
        return new(new(generation), (ExpandoObject)content, "added-hash", new(new(source), sourceIsAppended, Version));
    }

    protected Task Revise() => _storage.Revise(0, _eventType, CorrelationId.NotSet, [], [], DateTimeOffset.UtcNow, new ExpandoObject(), "revision-hash");

    protected async Task<string> Fingerprint()
    {
        await Task.CompletedTask;
        return JsonSerializer.Serialize(new[] { Stored(0).Content, Stored(0).ContentHashes, Stored(0).DerivedGenerations });
    }

    protected async Task<GenerationProvenance> Provenance(EventTypeGeneration generation)
    {
        await Task.CompletedTask;
        using var json = JsonDocument.Parse(Stored(0).DerivedGenerations!);
        var value = json.RootElement.GetProperty(generation.Value.ToString());
        return new(new(value.GetProperty("sourceGeneration").GetUInt32()), value.GetProperty("sourceIsAppended").GetBoolean(), new(value.GetProperty("migrationsVersion").GetString()!));
    }

    protected async Task ClearAppendedGeneration()
    {
        await using var context = CreateContext();
        var entry = context.Events.Single(_ => _.SequenceNumber == 0);
        entry.AppendedGeneration = null;
        await context.SaveChangesAsync();
    }

    protected async Task<string> Hash(EventTypeGeneration generation)
    {
        await Task.CompletedTask;
        return JsonSerializer.Deserialize<Dictionary<string, string>>(Stored(0).ContentHashes)![generation.Value.ToString()];
    }
}
