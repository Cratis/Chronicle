// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Seeding;

/// <summary>
/// Reconciles the two groupings a seed request carries, and shapes what the seed queries answer with.
/// </summary>
/// <remarks>
/// These live beside the artifacts rather than on them because a static method on a <c>[ReadModel]</c> whose
/// return shape is a supported query shape becomes a generated query proxy and an HTTP endpoint - accessibility
/// is not what the proxy generator looks at.
/// </remarks>
internal static class SeedingConverters
{
    /// <summary>
    /// Reconciles the two groupings a client sends - the same entries bucketed by event type and by event
    /// source - back into the single ordered list the seeders yielded.
    /// </summary>
    /// <param name="byEventSource">The entries as bucketed by event source - the grouping that decides the order.</param>
    /// <param name="byEventType">The entries as bucketed by event type.</param>
    /// <returns>Every entry, once per time it genuinely occurs, in the order its event source will see it.</returns>
    /// <remarks>
    /// The two groupings hold the same entries, so the reconciliation is a multiset union and not a
    /// deduplication: an entry occurring twice in each grouping occurs twice in the result. Collapsing on
    /// value instead would erase a genuine repeat - two events of the same type, on the same event source,
    /// with the same payload are two facts that really happened, not one fact sent twice - and an
    /// event-sourced store has no way to express that once it is gone. Taking each grouping's count and
    /// keeping the larger also copes with a client that fills in only one of them.
    /// <para>
    /// The by-event-source grouping leads because it is the only one that carries the order the seeders
    /// wrote. Both groupings hold the same entries, but bucketing by event type interleaves the streams:
    /// every entry of the first type, then every entry of the second. Appending in that order gives an
    /// event source a history it could never have lived through - submitted, submitted, approved, approved
    /// where the seeder said submitted, approved, submitted, approved. Bucketing by event source keeps each
    /// stream's entries in the sequence they were yielded, which is the sequence they are appended in.
    /// </para>
    /// </remarks>
    internal static List<Contracts.Seeding.SeedingEntry> Reconcile(IEnumerable<Contracts.Seeding.SeedingEntry> byEventSource, IEnumerable<Contracts.Seeding.SeedingEntry> byEventType)
    {
        var reconciled = byEventSource.ToList();

        var accountedFor = new Dictionary<Contracts.Seeding.SeedingEntry, int>(SeedingEntryIdentity.Comparer);
        foreach (var entry in reconciled)
        {
            accountedFor[entry] = accountedFor.GetValueOrDefault(entry) + 1;
        }

        foreach (var entry in byEventType)
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

    /// <summary>
    /// Reads the seeds a seeding grain holds and shapes them into the read model.
    /// </summary>
    /// <param name="grainFactory">The <see cref="IGrainFactory"/> to get the seeding grain with.</param>
    /// <param name="key">The <see cref="Concepts.Seeding.EventSeedingKey"/> identifying the grain.</param>
    /// <returns>The seed data.</returns>
    internal static async Task<SeedData> Read(IGrainFactory grainFactory, Concepts.Seeding.EventSeedingKey key)
    {
        var seeds = await grainFactory.GetGrain<IEventSeeding>(key.ToString()).GetSeededEvents();
        return ToSeedData(seeds);
    }

    /// <summary>
    /// Shapes the stored seeds into the read model the seed data queries answer with.
    /// </summary>
    /// <param name="seeds">The stored seeds.</param>
    /// <returns>The seeds grouped by event type and by event source.</returns>
    internal static SeedData ToSeedData(Storage.Seeding.EventSeeds seeds) =>
        new(
            [
                .. seeds.ByEventType.Select(group => new Contracts.Seeding.EventTypeSeedEntries
                {
                    EventTypeId = group.Key.Value,
                    Entries = [.. group.Value.Select(ToEntry)]
                })
            ],
            [
                .. seeds.ByEventSource.Select(group => new Contracts.Seeding.EventSourceSeedEntries
                {
                    EventSourceId = group.Key.Value,
                    Entries = [.. group.Value.Select(ToEntry)]
                })
            ]);

    static Contracts.Seeding.SeedingEntry ToEntry(Storage.Seeding.SeededEventEntry seeded) =>
        new()
        {
            EventSourceId = seeded.EventSourceId.Value,
            EventTypeId = seeded.EventTypeId.Value,
            Content = seeded.Content
        };

    /// <summary>
    /// Compares two entries on what identifies them: the event source, the event type, the content and the set of
    /// tags. It matches the comparison the seeding grain makes when it decides whether an entry has already been
    /// seeded, so the two never disagree about what "the same entry" means.
    /// </summary>
    sealed class SeedingEntryIdentity : IEqualityComparer<Contracts.Seeding.SeedingEntry>
    {
        internal static readonly SeedingEntryIdentity Comparer = new();

        public bool Equals(Contracts.Seeding.SeedingEntry? x, Contracts.Seeding.SeedingEntry? y) =>
            ReferenceEquals(x, y) ||
            (x is not null &&
             y is not null &&
             string.Equals(x.EventSourceId, y.EventSourceId, StringComparison.Ordinal) &&
             string.Equals(x.EventTypeId, y.EventTypeId, StringComparison.Ordinal) &&
             string.Equals(x.Content, y.Content, StringComparison.Ordinal) &&
             TagsOf(x).SetEquals(TagsOf(y)));

        public int GetHashCode(Contracts.Seeding.SeedingEntry obj) =>
            HashCode.Combine(obj.EventSourceId, obj.EventTypeId, obj.Content, TagsOf(obj).Count);

        static HashSet<string> TagsOf(Contracts.Seeding.SeedingEntry entry) => new(entry.Tags ?? [], StringComparer.Ordinal);
    }
}
