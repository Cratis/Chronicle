// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Storage.EventSequences;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences;

public partial class EventSequenceStorage
{
    /// <inheritdoc/>
    public async Task<StoredEventGenerations?> GetStoredGenerations(EventSequenceNumber sequenceNumber)
    {
        await using var scope = await database.EventSequenceTable(eventStore, @namespace, eventSequenceId);
        var number = sequenceNumber.Value;
        var stored = await scope.DbContext.Events.AsNoTracking().FirstOrDefaultAsync(_ => _.SequenceNumber == number);
        if (stored is null)
        {
            return null;
        }

        var content = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(stored.Content)!;

        return new(
            sequenceNumber,
            stored.Type,
            stored.EventSourceId,
            stored.Subject is { } subject ? new Subject(subject) : Subject.NotSet,
            stored.AppendedGeneration is { } appended ? new EventTypeGeneration(appended) : null,
            content.ToDictionary(_ => new EventTypeGeneration(uint.Parse(_.Key)), _ => _.Value.GetRawText()),
            0,
            JsonSerializer.Serialize(new[] { stored.Content, stored.ContentHashes }));
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

        var token = JsonSerializer.Deserialize<string[]>(observed.ConcurrencyToken)!;
        var tokenContent = token[0];
        var tokenHashes = token[1];
        var content = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(tokenContent)!;
        var hashes = string.IsNullOrEmpty(tokenHashes) ? [] : JsonSerializer.Deserialize<Dictionary<string, string>>(tokenHashes)!;
        if (additions.Any(_ => content.ContainsKey(_.Generation.Value.ToString())))
        {
            return false;
        }

        await using var scope = await database.EventSequenceTable(eventStore, @namespace, eventSequenceId);
        var number = observed.SequenceNumber.Value;
        var type = observed.EventTypeId;
        var stored = await scope.DbContext.Events.AsNoTracking().FirstOrDefaultAsync(_ => _.SequenceNumber == number);
        if (stored is null)
        {
            return false;
        }

        var provenance = string.IsNullOrEmpty(stored.DerivedGenerations) ? [] : JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(stored.DerivedGenerations)!;
        foreach (var addition in additions)
        {
            var generation = addition.Generation.Value.ToString();
            content.Add(generation, JsonSerializer.Deserialize<JsonElement>(EventEntryConverter.SerializeContent(addition.Content)));
            hashes[generation] = addition.Hash.Value;
            provenance[generation] = JsonSerializer.SerializeToElement(new
            {
                sourceGeneration = addition.Provenance.Source.Value,
                sourceIsAppended = addition.Provenance.SourceIsAppended,
                migrationsVersion = addition.Provenance.MigrationsVersion.Value
            });
        }
        var merged = JsonSerializer.Serialize(content);
        var mergedHashes = JsonSerializer.Serialize(hashes);
        var mergedProvenance = JsonSerializer.Serialize(provenance);
        var rows = await scope.DbContext.Events
            .Where(_ => _.SequenceNumber == number && _.Type == type && _.Content == tokenContent && _.ContentHashes == tokenHashes)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(_ => _.Content, merged)
                .SetProperty(_ => _.ContentHashes, mergedHashes)
                .SetProperty(_ => _.DerivedGenerations, mergedProvenance));

        return rows == 1;
    }
}
