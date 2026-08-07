// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Seeding;
using Cratis.Chronicle.Contracts.Seeding;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.Services.Seeding;

/// <summary>
/// Represents an implementation of <see cref="IEventSeeding"/>.
/// </summary>
/// <param name="grainFactory"><see cref="IGrainFactory"/> to get grains with.</param>
internal sealed class EventSeeding(IGrainFactory grainFactory) : IEventSeeding
{
    /// <inheritdoc/>
    public async Task Seed(SeedRequest request, CallContext context = default)
    {
        // Seed global entries to the global grain. The two groupings describe the same set of entries, so
        // they are reconciled rather than concatenated.
        var globalEntries = Reconcile(
            request.GlobalByEventType.SelectMany(_ => _.Entries),
            request.GlobalByEventSource.SelectMany(_ => _.Entries));

        if (globalEntries.Count > 0)
        {
            var globalKey = EventSeedingKey.ForGlobal(request.EventStore);
            var globalGrain = grainFactory.GetGrain<Chronicle.Seeding.IEventSeeding>(globalKey.ToString());

            var entries = globalEntries.Select(e => new Chronicle.Seeding.SeedingEntry(
                e.EventSourceId,
                e.EventTypeId,
                e.Content,
                e.Tags?.Select(t => new Concepts.Events.Tag(t)).ToArray() ?? [])).ToArray();

            await globalGrain.Seed(entries);
        }

        // Seed namespace-specific entries
        foreach (var namespacedGroup in request.NamespacedEntries)
        {
            var namespacedEntries = Reconcile(
                namespacedGroup.ByEventType.SelectMany(_ => _.Entries),
                namespacedGroup.ByEventSource.SelectMany(_ => _.Entries));

            if (namespacedEntries.Count > 0)
            {
                var key = EventSeedingKey.ForNamespace(request.EventStore, namespacedGroup.Namespace);
                var grain = grainFactory.GetGrain<Chronicle.Seeding.IEventSeeding>(key.ToString());

                var entries = namespacedEntries.Select(e => new Chronicle.Seeding.SeedingEntry(
                    e.EventSourceId,
                    e.EventTypeId,
                    e.Content,
                    e.Tags?.Select(t => new Concepts.Events.Tag(t)).ToArray() ?? [])).ToArray();

                await grain.Seed(entries);
            }
        }
    }

    /// <inheritdoc/>
    public async Task<SeedDataResponse> GetGlobalSeedData(GetSeedDataRequest request, CallContext context = default)
    {
        var key = EventSeedingKey.ForGlobal(request.EventStore);
        var grain = grainFactory.GetGrain<Chronicle.Seeding.IEventSeeding>(key.ToString());
        var seeds = await grain.GetSeededEvents();

        return MapToResponse(seeds);
    }

    /// <inheritdoc/>
    public async Task<SeedDataResponse> GetNamespaceSeedData(GetSeedDataRequest request, CallContext context = default)
    {
        var key = EventSeedingKey.ForNamespace(request.EventStore, request.Namespace);
        var grain = grainFactory.GetGrain<Chronicle.Seeding.IEventSeeding>(key.ToString());
        var seeds = await grain.GetSeededEvents();

        return MapToResponse(seeds);
    }

    /// <summary>
    /// Reconciles the two groupings a client sends - the same entries bucketed by event type and by event
    /// source - back into the single ordered list the seeders yielded.
    /// </summary>
    /// <param name="byEventType">The entries as bucketed by event type.</param>
    /// <param name="byEventSource">The entries as bucketed by event source.</param>
    /// <returns>Every entry, once per time it genuinely occurs.</returns>
    /// <remarks>
    /// The two groupings hold the same entries, so the reconciliation is a multiset union and not a
    /// deduplication: an entry occurring twice in each grouping occurs twice in the result. Collapsing on
    /// value instead would erase a genuine repeat - two events of the same type, on the same event source,
    /// with the same payload are two facts that really happened, not one fact sent twice - and an
    /// event-sourced store has no way to express that once it is gone. Taking each grouping's count and
    /// keeping the larger also copes with a client that fills in only one of them.
    /// </remarks>
    static List<SeedingEntry> Reconcile(IEnumerable<SeedingEntry> byEventType, IEnumerable<SeedingEntry> byEventSource)
    {
        var reconciled = byEventType.ToList();

        var accountedFor = new Dictionary<SeedingEntry, int>(SeedingEntryIdentity.Comparer);
        foreach (var entry in reconciled)
        {
            accountedFor[entry] = accountedFor.GetValueOrDefault(entry) + 1;
        }

        foreach (var entry in byEventSource)
        {
            var remaining = accountedFor.GetValueOrDefault(entry);
            if (remaining > 0)
            {
                accountedFor[entry] = remaining - 1;
                continue;
            }

            reconciled.Add(entry);
        }

        return reconciled;
    }

    static SeedDataResponse MapToResponse(Storage.Seeding.EventSeeds seeds)
    {
        var response = new SeedDataResponse();

        foreach (var (eventTypeId, entries) in seeds.ByEventType)
        {
            response.ByEventType.Add(new EventTypeSeedEntries
            {
                EventTypeId = eventTypeId.Value,
                Entries = entries.Select(e => new SeedingEntry
                {
                    EventSourceId = e.EventSourceId.Value,
                    EventTypeId = e.EventTypeId.Value,
                    Content = e.Content
                }).ToList()
            });
        }

        foreach (var (eventSourceId, entries) in seeds.ByEventSource)
        {
            response.ByEventSource.Add(new EventSourceSeedEntries
            {
                EventSourceId = eventSourceId.Value,
                Entries = entries.Select(e => new SeedingEntry
                {
                    EventSourceId = e.EventSourceId.Value,
                    EventTypeId = e.EventTypeId.Value,
                    Content = e.Content
                }).ToList()
            });
        }

        return response;
    }

    /// <summary>
    /// Compares two entries on what identifies them: the event source, the event type, the content and the
    /// set of tags. It matches the comparison the seeding grain makes when it decides whether an entry has
    /// already been seeded, so the two never disagree about what "the same entry" means.
    /// </summary>
    sealed class SeedingEntryIdentity : IEqualityComparer<SeedingEntry>
    {
        internal static readonly SeedingEntryIdentity Comparer = new();

        public bool Equals(SeedingEntry? x, SeedingEntry? y) =>
            ReferenceEquals(x, y) ||
            (x is not null &&
             y is not null &&
             string.Equals(x.EventSourceId, y.EventSourceId, StringComparison.Ordinal) &&
             string.Equals(x.EventTypeId, y.EventTypeId, StringComparison.Ordinal) &&
             string.Equals(x.Content, y.Content, StringComparison.Ordinal) &&
             TagsOf(x).SetEquals(TagsOf(y)));

        public int GetHashCode(SeedingEntry obj) =>
            HashCode.Combine(obj.EventSourceId, obj.EventTypeId, obj.Content, TagsOf(obj).Count);

        static HashSet<string> TagsOf(SeedingEntry entry) => new(entry.Tags ?? [], StringComparer.Ordinal);
    }
}
