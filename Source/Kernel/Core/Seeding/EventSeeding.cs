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
using Cratis.Chronicle.Namespaces;
using Cratis.Chronicle.Storage.Seeding;
using Microsoft.Extensions.Logging;
using Orleans.Providers;

namespace Cratis.Chronicle.Seeding;

/// <summary>
/// Represents an implementation of <see cref="IEventSeeding"/>.
/// </summary>
/// <param name="state">The <see cref="IPersistentState{T}"/> for the seeding data.</param>
/// <param name="grainFactory">The <see cref="IGrainFactory"/> for resolving namespace grains.</param>
/// <param name="logger">The <see cref="ILogger"/> for logging.</param>
[StorageProvider(ProviderName = WellKnownGrainStorageProviders.EventSeeding)]
public class EventSeeding(
    [PersistentState(nameof(EventSeeds), WellKnownGrainStorageProviders.EventSeeding)]
    IPersistentState<EventSeeds> state,
    IGrainFactory grainFactory,
    ILogger<EventSeeding> logger) : Grain, IEventSeeding
{
    /// <summary>
    /// The maximum number of events to append in a single batch during seeding.
    /// </summary>
    /// <remarks>
    /// Batching prevents overwhelming the event sequence grain when seeding a large number of events.
    /// </remarks>
    const int SeedingBatchSize = 100;

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

        // For global grains, store the entries and apply to all existing namespaces
        if (_key.IsGlobal)
        {
            var newEntries = new List<(SeedingEntry Entry, SeededEventEntry Seeded)>();
            foreach (var entry in entriesList)
            {
                var tags = entry.Tags?.Select(t => t.Value) ?? [];
                var seededEntry = new SeededEventEntry(entry.EventSourceId, entry.EventTypeId, entry.Content, tags);
                if (!IsAlreadySeeded(seededEntry))
                {
                    newEntries.Add((entry, seededEntry));
                }
            }

            if (newEntries.Count > 0)
            {
                // Dispatch to every namespace grain BEFORE committing the global tracking. Each namespace
                // grain applies its own idempotency guard, so re-dispatching after a transient failure is
                // append-idempotent. Committing the tracking first would permanently lose events: a grain
                // that throws mid-loop would never be retried - on retry IsAlreadySeeded would report the
                // entries as seeded, the dispatch would be skipped, and the remaining namespaces would
                // never receive the events.
                var entriesToSeed = newEntries.ConvertAll(_ => _.Entry);
                var namespacesGrain = grainFactory.GetGrain<INamespaces>(_key.EventStore.Value);
                var namespaces = await namespacesGrain.GetAll();
                foreach (var ns in namespaces)
                {
                    logger.ApplyingSeedsToNamespace(ns.Value);
                    var namespaceKey = EventSeedingKey.ForNamespace(_key.EventStore, ns);
                    var nsGrain = grainFactory.GetGrain<IEventSeeding>(namespaceKey.ToString());
                    await nsGrain.Seed(entriesToSeed);
                }

                // Only now, after every namespace has been seeded, commit the global tracking so a retry
                // re-dispatches when a namespace dispatch throws.
                foreach (var (_, seededEntry) in newEntries)
                {
                    TrackSeededEvent(seededEntry);
                }

                await state.WriteStateAsync();
            }
        }
        else
        {
            // For namespace-specific grains, append events to the sequence
            // _eventSequence is guaranteed to be non-null here since we're in the non-global branch
            if (_eventSequence is null)
            {
                throw new InvalidOperationException("Event sequence should be initialized for namespace-specific grains");
            }

            var seedableEvents = GetEventsToSeed(entriesList);
            if (seedableEvents.Count > 0)
            {
                logger.AppendingSeededEvents(seedableEvents.Count);
                var causation = new Causation[] { new(DateTimeOffset.UtcNow, "event-seeding", new Dictionary<string, string>()) };

                // Append in batches to avoid overwhelming the event sequence grain with too many events at once.
                // Mark each entry as seeded only AFTER its batch has been appended - never before. Tracking an
                // entry before the append succeeds would dirty the in-memory seeded set; a transient append
                // failure would then leave it claiming an event was seeded that was not, silently skipping the
                // event on a same-activation retry until the grain deactivates and reloads clean state.
                foreach (var batch in seedableEvents.Chunk(SeedingBatchSize))
                {
                    await _eventSequence.AppendMany(
                        batch.Select(_ => _.ToAppend),
                        CorrelationId.New(),
                        causation,
                        Identity.System,
                        new ConcurrencyScopes(new Dictionary<EventSourceId, ConcurrencyScope>()));

                    foreach (var seedable in batch)
                    {
                        TrackSeededEvent(seedable.Seeded);
                    }
                }

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

    List<SeedableEvent> GetEventsToSeed(List<SeedingEntry> entriesList)
    {
        var seedableEvents = new List<SeedableEvent>();

        foreach (var entry in entriesList)
        {
            var tags = entry.Tags?.Select(t => t.Value) ?? [];
            var seededEntry = new SeededEventEntry(entry.EventSourceId, entry.EventTypeId, entry.Content, tags);

            // Determine whether this exact event still needs seeding WITHOUT marking it seeded here -
            // the caller marks each entry as seeded only after its append has succeeded.
            var alreadySeeded = IsAlreadySeeded(seededEntry);

            if (!alreadySeeded)
            {
                // Prepare for appending
                var content = JsonSerializer.Deserialize<JsonObject>(entry.Content)!;
                seedableEvents.Add(new SeedableEvent(
                    seededEntry,
                    new EventToAppend(
                        EventSourceType.Default,
                        entry.EventSourceId,
                        EventStreamType.All,
                        EventStreamId.Default,
                        new EventType(entry.EventTypeId, EventTypeGeneration.First),
                        entry.Tags ?? [],
                        content)));
            }
        }

        return seedableEvents;
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

    /// <summary>
    /// Represents an event that still needs seeding, pairing the entry to append with the entry to
    /// record as seeded once the append has succeeded.
    /// </summary>
    /// <param name="Seeded">The <see cref="SeededEventEntry"/> to record once the event has been appended.</param>
    /// <param name="ToAppend">The <see cref="EventToAppend"/> to append to the event sequence.</param>
    record SeedableEvent(SeededEventEntry Seeded, EventToAppend ToAppend);
}
