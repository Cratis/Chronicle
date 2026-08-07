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
    public async Task<SeedingResult> Seed(IEnumerable<SeedingEntry> entries)
    {
        logger.SeedingEvents(_key.EventStore.Value, _key.Namespace.Value);

        var entriesList = entries.ToList();
        if (entriesList.Count == 0)
        {
            return SeedingResult.Complete;
        }

        state.State ??= new EventSeeds(
                new Dictionary<EventTypeId, IEnumerable<SeededEventEntry>>(),
                new Dictionary<EventSourceId, IEnumerable<SeededEventEntry>>());

        // For global grains, store the entries and apply to all existing namespaces
        return _key.IsGlobal
            ? await SeedGlobally(entriesList)
            : await SeedNamespace(entriesList);
    }

    /// <inheritdoc/>
    public Task<EventSeeds> GetSeededEvents()
    {
        state.State ??= new EventSeeds(
            new Dictionary<EventTypeId, IEnumerable<SeededEventEntry>>(),
            new Dictionary<EventSourceId, IEnumerable<SeededEventEntry>>());

        return Task.FromResult(state.State);
    }

    async Task<SeedingResult> SeedGlobally(List<SeedingEntry> entriesList)
    {
        var newEntries = GetEntriesStillToSeed(entriesList);
        if (newEntries.Count == 0)
        {
            return SeedingResult.Complete;
        }

        // Dispatch to every namespace grain BEFORE committing the global tracking. Each namespace
        // grain applies its own idempotency guard, so re-dispatching after a transient failure is
        // append-idempotent. Committing the tracking first would permanently lose events: a grain
        // that throws mid-loop would never be retried - on retry IsAlreadySeeded would report the
        // entries as seeded, the dispatch would be skipped, and the remaining namespaces would
        // never receive the events.
        var entriesToSeed = newEntries.ConvertAll(_ => _.Entry);
        var namespacesGrain = grainFactory.GetGrain<INamespaces>(_key.EventStore.Value);
        var namespaces = await namespacesGrain.GetAll();
        var everyNamespaceSeeded = true;
        foreach (var ns in namespaces)
        {
            logger.ApplyingSeedsToNamespace(ns.Value);
            var namespaceKey = EventSeedingKey.ForNamespace(_key.EventStore, ns);
            var nsGrain = grainFactory.GetGrain<IEventSeeding>(namespaceKey.ToString());
            var result = await nsGrain.Seed(entriesToSeed);
            everyNamespaceSeeded &= result.AllEntriesSeeded;
        }

        // A namespace that declined part of what it was given is the same situation as one that threw:
        // the entries are still waiting. Committing the tracking here would mean they are never
        // dispatched again, so hold it back and let the next run re-offer the whole set - the namespace
        // grains skip whatever already landed.
        if (!everyNamespaceSeeded)
        {
            logger.NamespaceSeedingIncomplete(_key.EventStore.Value);
            return SeedingResult.Incomplete;
        }

        // Only now, after every namespace has seeded everything, commit the global tracking so a retry
        // re-dispatches when a namespace dispatch throws or declines.
        foreach (var (_, seededEntry) in newEntries)
        {
            TrackSeededEvent(seededEntry);
        }

        await state.WriteStateAsync();

        return SeedingResult.Complete;
    }

    async Task<SeedingResult> SeedNamespace(List<SeedingEntry> entriesList)
    {
        // For namespace-specific grains, append events to the sequence
        // _eventSequence is guaranteed to be non-null here since we're in the non-global branch
        if (_eventSequence is null)
        {
            throw new InvalidOperationException("Event sequence should be initialized for namespace-specific grains");
        }

        var seedableEvents = GetEventsToSeed(entriesList);
        if (seedableEvents.Count == 0)
        {
            logger.AllEventsAlreadySeeded();
            return SeedingResult.Complete;
        }

        logger.AppendingSeededEvents(seedableEvents.Count);
        var causation = new Causation[] { new(DateTimeOffset.UtcNow, "event-seeding", new Dictionary<string, string>()) };

        // Append in batches to avoid overwhelming the event sequence grain with too many events at once.
        // Mark each entry as seeded only AFTER its batch has been appended - never before, and only when
        // the append actually succeeded. Appending many validates the whole batch before writing anything
        // and returns on the first failure, so a rejected batch means not one of its events exists;
        // recording it anyway is a permanent claim about events that were never written, and the entries
        // are then skipped on every later run. Leaving a rejected batch unrecorded is what makes offering
        // it again seed it, which is the only repair there is.
        var everyBatchAppended = true;
        var anythingAppended = false;
        foreach (var batch in seedableEvents.Chunk(SeedingBatchSize))
        {
            var result = await _eventSequence.AppendMany(
                batch.Select(_ => _.ToAppend),
                CorrelationId.New(),
                causation,
                Identity.System,
                new ConcurrencyScopes(new Dictionary<EventSourceId, ConcurrencyScope>()));

            if (!result.IsSuccess)
            {
                // Report and move on rather than throw. A rejected batch is usually a constraint violation,
                // which is deterministic - throwing would take the host down on every start and would also
                // strand the batches that did append, since their tracking is persisted below. The
                // remaining batches are unrelated events that share nothing with the rejected one but a
                // chunk index, so they are still worth appending.
                logger.SeededEventsRejected(
                    batch.Length,
                    _key.EventStore.Value,
                    _key.Namespace.Value,
                    string.Join(", ", result.ConstraintViolations.Select(_ => _.Message.Value)),
                    string.Join(", ", result.Errors.Select(_ => _.Value)),
                    string.Join(", ", result.ConcurrencyViolations.Select(_ => _.EventSourceId.Value)));

                everyBatchAppended = false;
                continue;
            }

            foreach (var seedable in batch)
            {
                TrackSeededEvent(seedable.Seeded);
            }

            anythingAppended = true;
        }

        if (anythingAppended)
        {
            await state.WriteStateAsync();
        }

        return everyBatchAppended ? SeedingResult.Complete : SeedingResult.Incomplete;
    }

    List<SeedableEvent> GetEventsToSeed(List<SeedingEntry> entriesList) =>
        GetEntriesStillToSeed(entriesList).ConvertAll(_ =>
        {
            var content = JsonSerializer.Deserialize<JsonObject>(_.Entry.Content)!;
            return new SeedableEvent(
                _.Seeded,
                new EventToAppend(
                    EventSourceType.Default,
                    _.Entry.EventSourceId,
                    EventStreamType.All,
                    EventStreamId.Default,
                    new EventType(_.Entry.EventTypeId, EventTypeGeneration.First),
                    _.Entry.Tags ?? [],
                    content));
        });

    /// <summary>
    /// Filters the entries down to the ones the seeded tracking does not already account for, WITHOUT
    /// marking any of them seeded - the caller does that only once the append has succeeded.
    /// </summary>
    /// <param name="entriesList">The entries offered for seeding, in order.</param>
    /// <returns>The entries still to seed, paired with the entry to record for each.</returns>
    /// <remarks>
    /// The tracking is consulted by COUNT, not by existence. Two events of the same type, on the same
    /// event source, with the same payload are two facts that really happened; asking only whether an
    /// equal entry has been seeded would skip the second one forever as soon as the first had landed -
    /// which is exactly the state a rejected batch or a chunk boundary between the two leaves behind, so
    /// an existence check would quietly cancel the retry that is supposed to repair it.
    /// </remarks>
    List<(SeedingEntry Entry, SeededEventEntry Seeded)> GetEntriesStillToSeed(List<SeedingEntry> entriesList)
    {
        var stillToSeed = new List<(SeedingEntry Entry, SeededEventEntry Seeded)>();
        var stillAccountedFor = new Dictionary<SeededEventEntry, int>(SeededEventEntryIdentity.Comparer);

        foreach (var entry in entriesList)
        {
            var tags = entry.Tags?.Select(t => t.Value) ?? [];
            var seededEntry = new SeededEventEntry(entry.EventSourceId, entry.EventTypeId, entry.Content, tags);

            if (!stillAccountedFor.TryGetValue(seededEntry, out var remaining))
            {
                remaining = CountAlreadySeeded(seededEntry);
            }

            if (remaining > 0)
            {
                stillAccountedFor[seededEntry] = remaining - 1;
                continue;
            }

            stillAccountedFor[seededEntry] = 0;
            stillToSeed.Add((entry, seededEntry));
        }

        return stillToSeed;
    }

    int CountAlreadySeeded(SeededEventEntry entry)
    {
        var byType = state.State.ByEventType.TryGetValue(entry.EventTypeId, out var byTypeEntries)
            ? byTypeEntries.Count(e => SeededEventEntryIdentity.Comparer.Equals(e, entry))
            : 0;

        var bySource = state.State.ByEventSource.TryGetValue(entry.EventSourceId, out var bySourceEntries)
            ? bySourceEntries.Count(e => SeededEventEntryIdentity.Comparer.Equals(e, entry))
            : 0;

        // The two halves are written together and should agree; taking the larger keeps the guard on the
        // safe side of a half that was written by an older version or lost a write.
        return Math.Max(byType, bySource);
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

    /// <summary>
    /// Compares two seeded entries on what identifies them: the event source, the event type, the content
    /// and the set of tags. The record's own equality cannot be used - it compares the tag sequence by
    /// reference, so two entries with equal tags never match.
    /// </summary>
    sealed class SeededEventEntryIdentity : IEqualityComparer<SeededEventEntry>
    {
        internal static readonly SeededEventEntryIdentity Comparer = new();

        public bool Equals(SeededEventEntry? x, SeededEventEntry? y) =>
            ReferenceEquals(x, y) ||
            (x is not null &&
             y is not null &&
             x.EventSourceId == y.EventSourceId &&
             x.EventTypeId == y.EventTypeId &&
             string.Equals(x.Content, y.Content, StringComparison.Ordinal) &&
             TagsOf(x).SetEquals(TagsOf(y)));

        public int GetHashCode(SeededEventEntry obj) =>
            HashCode.Combine(obj.EventSourceId, obj.EventTypeId, obj.Content, TagsOf(obj).Count);

        static HashSet<string> TagsOf(SeededEventEntry entry) => new(entry.Tags ?? [], StringComparer.Ordinal);
    }
}
