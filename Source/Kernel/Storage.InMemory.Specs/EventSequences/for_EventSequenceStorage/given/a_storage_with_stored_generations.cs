// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Reflection;
using System.Text.Json;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Storage.EventSequences;

namespace Cratis.Chronicle.Storage.InMemory.EventSequences.for_EventSequenceStorage.given;

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
        return JsonSerializer.Serialize(new { Content = _storage.Events.Single().GenerationalContent, Hashes = _storage.GetGenerationHashes(0).ToDictionary(_ => _.Key.Value, _ => _.Value.Value), Provenance = _storage.GetDerivedGenerations(0).ToDictionary(_ => _.Key.Value, _ => _.Value) });
    }

    protected async Task<GenerationProvenance> Provenance(EventTypeGeneration generation)
    {
        await Task.CompletedTask;
        return _storage.GetDerivedGenerations(0)[generation];
    }

    protected async Task ClearAppendedGeneration()
    {
        var metadata = (IDictionary<EventSequenceNumber, uint>)typeof(EventSequenceStorage)
            .GetField("_appendedGenerations", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(_storage)!;
        metadata.Clear();
        await Task.CompletedTask;
    }

    protected Task<string> Hash(EventTypeGeneration generation) => Task.FromResult(_storage.GetGenerationHashes(0)[generation].Value);
}
