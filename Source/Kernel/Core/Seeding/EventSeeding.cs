// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts.Auditing;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.EventSequences.Concurrency;
using Cratis.Chronicle.Concepts.Identities;
using Cratis.Chronicle.Concepts.Seeding;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Storage.Seeding;
using Microsoft.Extensions.Logging;
using Orleans.Providers;

namespace Cratis.Chronicle.Seeding;

/// <summary>
/// Represents an implementation of <see cref="IEventSeeding"/>.
/// </summary>
/// <param name="state">The <see cref="IPersistentState{T}"/> for the seeding data.</param>
/// <param name="logger">The <see cref="ILogger"/> for logging.</param>
[StorageProvider(ProviderName = WellKnownGrainStorageProviders.EventSeeding)]
public class EventSeeding(
    [PersistentState(nameof(EventSeeds), WellKnownGrainStorageProviders.EventSeeding)]
    IPersistentState<EventSeeds> state,
    ILogger<EventSeeding> logger) : Grain, IEventSeeding
{
    EventSeedingKey _key = EventSeedingKey.NotSet;
    IEventSequence? _eventSequence;

    /// <inheritdoc/>
    public override Task OnActivateAsync(CancellationToken cancellationToken)
    {
        _key = EventSeedingKey.Parse(this.GetPrimaryKeyString());

        // Only create event sequence for namespace-specific grains
        if (!_key.IsGlobal)
        {
            _eventSequence = GrainFactory.GetGrain<IEventSequence>(
                new EventSequenceKey(EventSequenceId.Log, _key.EventStore, _key.Namespace));
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public async Task Seed(IEnumerable<SeedingEntry> entries)
    {
        logger.SeedingEvents(_key.EventStore.Value, _key.Namespace.Value);

        var entriesList = entries.ToList();
        if (entriesList.Count == 0)
        {
            return;
        }

        state.State ??= new EventSeeds(
                new Dictionary<EventTypeId, IEnumerable<SeededEventEntry>>(),
                new Dictionary<EventSourceId, IEnumerable<SeededEventEntry>>());

        // For global grains, just store the entries without appending
        if (_key.IsGlobal)
        {
            foreach (var entry in entriesList)
            {
                var tags = entry.Tags?.Select(t => t.Value) ?? [];
                var seededEntry = new SeededEventEntry(entry.EventSourceId, entry.EventTypeId, entry.Content, tags);
                if (!IsAlreadySeeded(seededEntry))
                {
                    TrackSeededEvent(seededEntry);
                }
            }
            await state.WriteStateAsync();
        }
        else
        {
            // For namespace-specific grains, append events to the sequence
            // _eventSequence is guaranteed to be non-null here since we're in the non-global branch
            if (_eventSequence is null)
            {
                throw new InvalidOperationException("Event sequence should be initialized for namespace-specific grains");
            }

            var eventsToAppend = GetEventsToSeed(entriesList);
            if (eventsToAppend.Count > 0)
            {
                logger.AppendingSeededEvents(eventsToAppend.Count);
                var causation = new Causation[] { new(DateTimeOffset.UtcNow, "event-seeding", new Dictionary<string, string>()) };

                await _eventSequence.AppendMany(
                    eventsToAppend,
                    CorrelationId.New(),
                    causation,
                    Identity.System,
                    new ConcurrencyScopes(new Dictionary<EventSourceId, ConcurrencyScope>()));

                await state.WriteStateAsync();
            }
            else
            {
                logger.AllEventsAlreadySeeded();
            }
        }
    }

    /// <inheritdoc/>
    public Task<EventSeeds> GetSeededEvents()
    {
        state.State ??= new EventSeeds(
            new Dictionary<EventTypeId, IEnumerable<SeededEventEntry>>(),
            new Dictionary<EventSourceId, IEnumerable<SeededEventEntry>>());

        return Task.FromResult(state.State);
    }

    List<EventToAppend> GetEventsToSeed(List<SeedingEntry> entriesList)
    {
        var eventsToAppend = new List<EventToAppend>();

        foreach (var entry in entriesList)
        {
            var tags = entry.Tags?.Select(t => t.Value) ?? [];
            var seededEntry = new SeededEventEntry(entry.EventSourceId, entry.EventTypeId, entry.Content, tags);

            // Check if this exact event has already been seeded
            var alreadySeeded = IsAlreadySeeded(seededEntry);

            if (!alreadySeeded)
            {
                // Add to storage tracking
                TrackSeededEvent(seededEntry);

                // Prepare for appending
                var content = JsonSerializer.Deserialize<JsonObject>(entry.Content)!;
                eventsToAppend.Add(new EventToAppend(
                    EventSourceType.Default,
                    entry.EventSourceId,
                    EventStreamType.All,
                    EventStreamId.Default,
                    new EventType(entry.EventTypeId, EventTypeGeneration.First),
                    entry.Tags ?? [],
                    content));
            }
        }

        return eventsToAppend;
    }

    bool IsAlreadySeeded(SeededEventEntry entry)
    {
        var entryTagsSet = new HashSet<string>(entry.Tags ?? []);

        // Check in ByEventType
        if (state.State.ByEventType.TryGetValue(entry.EventTypeId, out var byTypeEntries) &&
            byTypeEntries.Any(e => e.EventSourceId == entry.EventSourceId &&
                                      e.EventTypeId == entry.EventTypeId &&
                                      e.Content == entry.Content &&
                                      new HashSet<string>(e.Tags ?? []).SetEquals(entryTagsSet)))
        {
            return true;
        }

        // Check in ByEventSource
        if (state.State.ByEventSource.TryGetValue(entry.EventSourceId, out var bySourceEntries) &&
            bySourceEntries.Any(e => e.EventSourceId == entry.EventSourceId &&
                                        e.EventTypeId == entry.EventTypeId &&
                                        e.Content == entry.Content &&
                                        new HashSet<string>(e.Tags ?? []).SetEquals(entryTagsSet)))
        {
            return true;
        }

        return false;
    }

    void TrackSeededEvent(SeededEventEntry entry)
    {
#pragma warning disable CA1854 // Prefer the 'IDictionary.TryGetValue(TKey, out TValue)' method
        if (!state.State.ByEventType.ContainsKey(entry.EventTypeId))
        {
            state.State.ByEventType[entry.EventTypeId] = [];
        }

        state.State.ByEventType[entry.EventTypeId] = [.. state.State.ByEventType[entry.EventTypeId], .. new SeededEventEntry[] { entry }];

        // Track by event source
        if (!state.State.ByEventSource.ContainsKey(entry.EventSourceId))
        {
            state.State.ByEventSource[entry.EventSourceId] = [];
        }

        state.State.ByEventSource[entry.EventSourceId] = [.. state.State.ByEventSource[entry.EventSourceId], .. new SeededEventEntry[] { entry }];
#pragma warning restore CA1854 // Prefer the 'IDictionary.TryGetValue(TKey, out TValue)' method
    }
}
