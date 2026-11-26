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
/// <param name="timeProvider">The clock for constraint index retry delays, or the system clock when omitted.</param>
[StorageProvider(ProviderName = WellKnownGrainStorageProviders.EventSeeding)]
public class EventSeeding(
    [PersistentState(nameof(EventSeeds), WellKnownGrainStorageProviders.EventSeeding)]
    IPersistentState<EventSeeds> state,
    IGrainFactory grainFactory,
    ILogger<EventSeeding> logger,
    TimeProvider? timeProvider = null) : Grain, IGlobalEventSeeding
{
    /// <summary>
    /// The maximum number of events to append in a single batch during seeding.
    /// </summary>
    /// <remarks>
    /// Batching prevents overwhelming the event sequence grain when seeding a large number of events.
    /// </remarks>
    const int SeedingBatchSize = 100;

    /// <summary>
    /// The maximum number of retry delays within a namespace, also capped by the run-wide time budget.
    /// </summary>
    const int MaxIndexWaitDelays = 4;

    /// <summary>
    /// Carries the remaining run-wide wait allowance to a namespace grain without changing its contract.
    /// </summary>
    const string IndexWaitBudgetKey = "Cratis.Chronicle.Seeding.IndexWaitBudgetTicks";

    /// <summary>
    /// Caps intentional index waits across a run, including namespace dispatches, at four seconds.
    /// Append and dispatch execution time is not bounded by this retry budget.
    /// </summary>
    static readonly TimeSpan _maxIndexWait = TimeSpan.FromSeconds(4);

    EventSeedingKey _key = EventSeedingKey.NotSet;
    IEventSequence? _eventSequence;

    TimeProvider Clock => timeProvider ?? TimeProvider.System;

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
    public Task Seed(IEnumerable<SeedingEntry> entries) => SeedWithResult(entries);

    /// <inheritdoc/>
    public Task<SeedingResult> SeedWithResult(IEnumerable<SeedingEntry> entries) => SeedWithResult(entries, false, string.Empty);

    /// <inheritdoc/>
    public Task<SeedingResult> SeedGlobalWithResult(IEnumerable<SeedingEntry> entries, bool authoritative, string owner) =>
        SeedWithResult(entries, authoritative, owner);

    /// <inheritdoc/>
    public Task<EventSeeds> GetSeededEvents()
    {
        state.State ??= new EventSeeds(
            new Dictionary<EventTypeId, IEnumerable<SeededEventEntry>>(),
            new Dictionary<EventSourceId, IEnumerable<SeededEventEntry>>());

        return Task.FromResult(_key.IsGlobal ? GlobalSeedSet.Effective(state.State) : state.State);
    }

    static SeededEventEntry ToSeededEntry(SeedingEntry entry) => new(entry.EventSourceId, entry.EventTypeId, entry.Content, entry.Tags?.Select(t => t.Value) ?? [])
    {
        EventSourceType = SeedRouting.SourceType(entry.EventSourceType?.Value),
        EventStreamType = SeedRouting.StreamType(entry.EventStreamType?.Value),
        EventStreamId = SeedRouting.StreamId(entry.EventStreamId?.Value)
    };

    async Task<SeedingResult> SeedWithResult(IEnumerable<SeedingEntry> entries, bool authoritative, string owner)
    {
        logger.SeedingEvents(_key.EventStore.Value, _key.Namespace.Value);

        var entriesList = entries.ToList();
        if (entriesList.Count == 0 && !(_key.IsGlobal && authoritative))
        {
            return SeedingResult.Complete;
        }

        state.State ??= new EventSeeds(
                new Dictionary<EventTypeId, IEnumerable<SeededEventEntry>>(),
                new Dictionary<EventSourceId, IEnumerable<SeededEventEntry>>());

        var remaining = RequestContext.Get(IndexWaitBudgetKey) is long ticks
            ? TimeSpan.FromTicks(Math.Clamp(ticks, 0, _maxIndexWait.Ticks))
            : _maxIndexWait;
        var waitBudget = new IndexWaitBudget(remaining);

        // For global grains, store the entries and apply to all existing namespaces
        return _key.IsGlobal
            ? await SeedGlobally(entriesList, authoritative, owner, waitBudget)
            : await SeedNamespace(entriesList, waitBudget);
    }

    async Task<SeedingResult> SeedGlobally(List<SeedingEntry> entriesList, bool authoritative, string owner, IndexWaitBudget waitBudget)
    {
        var offered = entriesList.Select(entry => ToSeededEntry(entry) with
        {
            Origin = authoritative ? SeedOrigin.Declared : SeedOrigin.Added,
            Owner = owner
        });
        var updated = authoritative
            ? GlobalSeedSet.Replace(state.State, offered, owner)
            : GlobalSeedSet.Add(state.State, offered, owner);
        if (ReferenceEquals(updated, state.State))
        {
            return SeedingResult.Complete;
        }

        var growingEntries = GlobalSeedSet.GrowingEntries(state.State, updated)
            .Select(entry => new SeedingEntry(entry.EventSourceId, entry.EventTypeId, entry.Content, entry.Tags?.Select(tag => new Tag(tag)), entry.EventSourceType, entry.EventStreamType, entry.EventStreamId))
            .ToList();

        // Dispatch to every namespace grain BEFORE committing the global tracking. Each namespace
        // grain applies its own idempotency guard, so re-dispatching after a transient failure is
        // append-idempotent. Committing the tracking first would permanently lose events: a grain
        // that throws mid-loop would never be retried - on retry IsAlreadySeeded would report the
        // entries as seeded, the dispatch would be skipped, and the remaining namespaces would
        // never receive the events.
        var everyNamespaceSeeded = await DispatchGlobally(growingEntries, waitBudget);

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
        state.State = updated;

        await state.WriteStateAsync();

        return SeedingResult.Complete;
    }

    async Task<bool> DispatchGlobally(List<SeedingEntry> entries, IndexWaitBudget waitBudget)
    {
        if (entries.Count == 0)
        {
            return true;
        }

        var namespacesGrain = grainFactory.GetGrain<INamespaces>(_key.EventStore.Value);
        var namespaces = await namespacesGrain.GetAll();
        var everyNamespaceSeeded = true;
        foreach (var ns in namespaces)
        {
            logger.ApplyingSeedsToNamespace(ns.Value);
            var namespaceKey = EventSeedingKey.ForNamespace(_key.EventStore, ns);
            var nsGrain = grainFactory.GetGrain<IResultAwareEventSeeding>(namespaceKey.ToString());
            var result = await DispatchNamespaceWithBudget(nsGrain, entries, waitBudget);
            everyNamespaceSeeded &= result.AllEntriesSeeded;
        }

        return everyNamespaceSeeded;
    }

    async Task<SeedingResult> DispatchNamespaceWithBudget(IResultAwareEventSeeding namespaceGrain, List<SeedingEntry> entries, IndexWaitBudget waitBudget)
    {
        var previous = RequestContext.Get(IndexWaitBudgetKey);
        RequestContext.Set(IndexWaitBudgetKey, waitBudget.Remaining.Ticks);
        var started = Clock.GetTimestamp();
        try
        {
            return await namespaceGrain.SeedWithResult(entries);
        }
        finally
        {
            // Request context flows to the namespace grain, not back from it. Charging the whole call's
            // elapsed time conservatively accounts for its waits without changing the grain contract.
            waitBudget.ChargeElapsed(Clock.GetElapsedTime(started));
            if (previous is null)
            {
                RequestContext.Remove(IndexWaitBudgetKey);
            }
            else
            {
                RequestContext.Set(IndexWaitBudgetKey, previous);
            }
        }
    }

    async Task<SeedingResult> SeedNamespace(List<SeedingEntry> entriesList, IndexWaitBudget waitBudget)
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
            var result = await AppendBatchWithIndexWait(_eventSequence, batch, causation, waitBudget);

            if (!result.IsSuccess)
            {
                // Report and move on rather than throw. A rejected batch is usually a constraint violation,
                // which is deterministic - throwing would take the host down on every start and would also
                // strand the batches that did append, since their tracking is persisted below. The
                // remaining batches are unrelated events that share nothing with the rejected one but a
                // chunk index, so they are still worth appending.
                ReportSeedBatchRefusal(batch.Length, result);

                everyBatchAppended = false;
                continue;
            }

            TrackSeededBatch(batch);

            anythingAppended = true;
        }

        if (anythingAppended)
        {
            await state.WriteStateAsync();
        }

        return everyBatchAppended ? SeedingResult.Complete : SeedingResult.Incomplete;
    }

    async Task<AppendManyResult> AppendBatchWithIndexWait(IEventSequence eventSequence, SeedableEvent[] batch, Causation[] causation, IndexWaitBudget waitBudget)
    {
        while (true)
        {
            var result = await eventSequence.AppendMany(
                batch.Select(_ => _.ToAppend),
                CorrelationId.New(),
                causation,
                Identity.System,
                new ConcurrencyScopes(new Dictionary<EventSourceId, ConcurrencyScope>()));

            var delay = result.RetryAfter ?? TimeSpan.FromSeconds(1);
            if (!result.IsRetryable || !waitBudget.TryReserve(delay))
            {
                return result;
            }

            await Task.Delay(delay, Clock);
        }
    }

    void ReportSeedBatchRefusal(int count, AppendManyResult result)
    {
        if (result.IsRetryable)
        {
            logger.SeededEventsDeferredByConstraintIndex(count, _key.EventStore.Value, _key.Namespace.Value);
            return;
        }

        // Counts and constraint names only. An event source id is the compliance subject by default, and
        // a constraint violation message embeds it, so neither belongs in a log line that outlives the
        // subject's erasure.
        logger.SeededEventsRejected(
            count,
            _key.EventStore.Value,
            _key.Namespace.Value,
            string.Join(", ", result.ConstraintViolations.Select(_ => _.ConstraintName.Value).Distinct()),
            result.Errors.Count(),
            result.ConcurrencyViolations.Count());
    }

    void TrackSeededBatch(SeedableEvent[] batch)
    {
        foreach (var seedable in batch)
        {
            TrackSeededEvent(seedable.Seeded);
        }
    }

    List<SeedableEvent> GetEventsToSeed(List<SeedingEntry> entriesList) =>
        GetEntriesStillToSeed(entriesList).ConvertAll(_ =>
        {
            var content = JsonSerializer.Deserialize<JsonObject>(_.Entry.Content)!;
            return new SeedableEvent(
                _.Seeded,
                new EventToAppend(
                    _.Seeded.EventSourceType,
                    _.Entry.EventSourceId,
                    _.Seeded.EventStreamType,
                    _.Seeded.EventStreamId,
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
            var seededEntry = ToSeededEntry(entry);

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
    /// Shares a finite retry allowance across chunks and accounts for time spent dispatching namespaces.
    /// </summary>
    /// <param name="remaining">The retry wait time available to this run.</param>
    sealed class IndexWaitBudget(TimeSpan remaining)
    {
        int _remainingDelays = MaxIndexWaitDelays;

        /// <summary>
        /// Gets the retry wait time still available.
        /// </summary>
        public TimeSpan Remaining { get; private set; } = remaining;

        /// <summary>
        /// Reserves a delay only when it fits within the remaining run-wide allowance.
        /// </summary>
        /// <param name="delay">The suggested retry delay.</param>
        /// <returns>Whether the delay was reserved.</returns>
        public bool TryReserve(TimeSpan delay)
        {
            if (delay <= TimeSpan.Zero)
            {
                return false;
            }

            if (_remainingDelays == 0 || delay > Remaining)
            {
                return false;
            }

            _remainingDelays--;
            Remaining -= delay;
            return true;
        }

        /// <summary>
        /// Conservatively charges a namespace call's elapsed time against the shared allowance.
        /// </summary>
        /// <param name="elapsed">The duration of the namespace dispatch.</param>
        public void ChargeElapsed(TimeSpan elapsed) => Remaining = TimeSpan.FromTicks(Math.Max(0, Remaining.Ticks - elapsed.Ticks));
    }

    /// <summary>
    /// Represents an event that still needs seeding, pairing the entry to append with the entry to
    /// record as seeded once the append has succeeded.
    /// </summary>
    /// <param name="Seeded">The <see cref="SeededEventEntry"/> to record once the event has been appended.</param>
    /// <param name="ToAppend">The <see cref="EventToAppend"/> to append to the event sequence.</param>
    record SeedableEvent(SeededEventEntry Seeded, EventToAppend ToAppend);
}
