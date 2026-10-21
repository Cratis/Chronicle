// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Storage.EventSequences;

namespace Cratis.Chronicle.Storage.InMemory.EventSequences;

public partial class EventSequenceStorage
{
    readonly Dictionary<EventSequenceNumber, Dictionary<EventTypeGeneration, EventHash>> _generationHashes = [];
    readonly Dictionary<EventSequenceNumber, Dictionary<EventTypeGeneration, GenerationProvenance>> _derivedGenerations = [];

    /// <summary>
    /// Reads the hashes persisted alongside each generation.
    /// </summary>
    /// <param name="sequenceNumber">The event to read.</param>
    /// <returns>A snapshot of the hashes.</returns>
    public IReadOnlyDictionary<EventTypeGeneration, EventHash> GetGenerationHashes(EventSequenceNumber sequenceNumber)
    {
        lock (_lock)
        {
            return _generationHashes.TryGetValue(sequenceNumber, out var hashes) ? hashes.ToImmutableDictionary() : [];
        }
    }

    /// <summary>
    /// Reads the provenance persisted for backfilled generations.
    /// </summary>
    /// <param name="sequenceNumber">The event to read.</param>
    /// <returns>A snapshot of the provenance.</returns>
    public IReadOnlyDictionary<EventTypeGeneration, GenerationProvenance> GetDerivedGenerations(EventSequenceNumber sequenceNumber)
    {
        lock (_lock)
        {
            return _derivedGenerations.TryGetValue(sequenceNumber, out var provenance) ? provenance.ToImmutableDictionary() : [];
        }
    }

    /// <inheritdoc/>
    public Task<StoredEventGenerations?> GetStoredGenerations(EventSequenceNumber sequenceNumber)
    {
        lock (_lock)
        {
            var stored = _events.Find(_ => _.Context.SequenceNumber == sequenceNumber);
            if (stored is null)
            {
                return Task.FromResult<StoredEventGenerations?>(null);
            }

            var content = stored.GenerationalContent.ToDictionary(_ => new EventTypeGeneration((uint)_.Key), _ => _.Value);
            EventTypeGeneration? appended = _appendedGenerations.TryGetValue(sequenceNumber, out var generation) ? new(generation) : null;

            return Task.FromResult<StoredEventGenerations?>(new(
                sequenceNumber,
                stored.Context.EventType.Id,
                stored.Context.EventSourceId,
                stored.Context.Subject ?? Subject.NotSet,
                appended,
                content,
                stored.Revisions.Count(),
                string.Empty));
        }
    }

    /// <inheritdoc/>
    public Task<bool> TryAddGenerations(StoredEventGenerations observed, IEnumerable<GenerationToAdd> generations)
    {
        var additions = generations.ToArray();
        lock (_lock)
        {
            var index = _events.FindIndex(_ => _.Context.SequenceNumber == observed.SequenceNumber);
            if (index < 0 || additions.Length == 0 || additions.Select(_ => _.Generation).Distinct().Count() != additions.Length)
            {
                return Task.FromResult(false);
            }

            var stored = _events[index];
            if (stored.Context.EventType.Id != observed.EventTypeId || stored.Context.EventType.Id == GlobalEventTypes.Redaction ||
                stored.Revisions.Count() != observed.RevisionCount ||
                additions.Any(_ => stored.GenerationalContent.ContainsKey((int)_.Generation.Value)) ||
                observed.Content.Any(_ => !stored.GenerationalContent.TryGetValue((int)_.Key.Value, out var json) || json != _.Value))
            {
                return Task.FromResult(false);
            }

            var content = stored.GenerationalContent.ToDictionary();
            var hashes = _generationHashes.TryGetValue(observed.SequenceNumber, out var storedHashes) ? new Dictionary<EventTypeGeneration, EventHash>(storedHashes) : [];
            var provenance = _derivedGenerations.TryGetValue(observed.SequenceNumber, out var storedProvenance) ? new Dictionary<EventTypeGeneration, GenerationProvenance>(storedProvenance) : [];
            foreach (var addition in additions)
            {
                content.Add((int)addition.Generation.Value, Serialize(addition.Content));
                hashes[addition.Generation] = addition.Hash;
                provenance[addition.Generation] = addition.Provenance;
            }
            _generationHashes[observed.SequenceNumber] = hashes;
            _derivedGenerations[observed.SequenceNumber] = provenance;
            var updated = stored with
            {
                GenerationalContent = content,
                GenerationalHashes = hashes.ToDictionary(_ => (int)_.Key.Value, _ => _.Value)
            };
            if (!stored.IsRevised)
            {
                var highest = additions.MaxBy(_ => _.Generation.Value)!;
                if (highest.Generation > stored.Context.EventType.Generation)
                {
                    updated = updated with
                    {
                        Content = highest.Content,
                        Context = stored.Context with { EventType = stored.Context.EventType with { Generation = highest.Generation }, Hash = highest.Hash }
                    };
                }
            }
            _events[index] = updated;

            return Task.FromResult(true);
        }
    }
}
