// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Seeding;

namespace Cratis.Chronicle.Seeding;

/// <summary>
/// Maintains global seed provenance and computes the effective multiset across programs and origins.
/// </summary>
internal static class GlobalSeedSet
{
    /// <summary>
    /// Replaces the owner's declared seeds and legacy entries with the current declaration.
    /// </summary>
    /// <param name="current">The currently stored seed set.</param>
    /// <param name="declared">The owner's current declaration.</param>
    /// <param name="owner">The program declaring its seeds.</param>
    /// <returns>The updated global seed set.</returns>
    internal static EventSeeds Replace(EventSeeds current, IEnumerable<SeededEventEntry> declared, string owner)
    {
        var entries = declared.ToArray();
        return new(
            ReplaceGrouping(current.ByEventType, entries.GroupBy(_ => _.EventTypeId), owner),
            ReplaceGrouping(current.ByEventSource, entries.GroupBy(_ => _.EventSourceId), owner));
    }

    /// <summary>
    /// Records explicit additions by their own provenance count, even when another group covers them.
    /// </summary>
    /// <param name="current">The currently stored seed set.</param>
    /// <param name="added">The entries added by this request.</param>
    /// <param name="owner">The program adding the seeds.</param>
    /// <returns>The updated set, or the current set if the additions were already recorded.</returns>
    internal static EventSeeds Add(EventSeeds current, IEnumerable<SeededEventEntry> added, string owner)
    {
        var entries = added.ToArray();
        var updated = new EventSeeds(
            AddGrouping(current.ByEventType, entries.GroupBy(_ => _.EventTypeId), owner),
            AddGrouping(current.ByEventSource, entries.GroupBy(_ => _.EventSourceId), owner));
        return updated.ByEventType.Values.Sum(_ => _.Count()) == current.ByEventType.Values.Sum(_ => _.Count()) &&
            updated.ByEventSource.Values.Sum(_ => _.Count()) == current.ByEventSource.Values.Sum(_ => _.Count())
            ? current
            : updated;
    }

    /// <summary>
    /// Uses the maximum identity count across provenance groups, rather than their sum.
    /// </summary>
    /// <param name="stored">The set containing all provenance groups.</param>
    /// <returns>The effective seed multiset for a namespace.</returns>
    internal static EventSeeds Effective(EventSeeds stored) => new(
        EffectiveGrouping(stored.ByEventType),
        EffectiveGrouping(stored.ByEventSource));

    /// <summary>
    /// Offers full new counts only for identities whose effective count grew, so namespace count-based
    /// idempotency appends missing occurrences and safely retries partial dispatches.
    /// </summary>
    /// <param name="before">The stored set before registration.</param>
    /// <param name="after">The proposed stored set after registration.</param>
    /// <returns>The entries to offer to existing namespaces.</returns>
    internal static IEnumerable<SeededEventEntry> GrowingEntries(EventSeeds before, EventSeeds after)
    {
        var previous = Effective(before);
        return Effective(after).ByEventSource.Values.SelectMany(_ => _)
            .GroupBy(_ => _, SeededEventEntryIdentity.Comparer)
            .Where(group => group.Count() > Count(previous, group.Key))
            .SelectMany(_ => _);
    }

    static int Count(EventSeeds seeds, SeededEventEntry entry)
    {
        var byType = seeds.ByEventType.TryGetValue(entry.EventTypeId, out var types)
            ? types.Count(_ => SeededEventEntryIdentity.Comparer.Equals(_, entry)) : 0;
        var bySource = seeds.ByEventSource.TryGetValue(entry.EventSourceId, out var sources)
            ? sources.Count(_ => SeededEventEntryIdentity.Comparer.Equals(_, entry)) : 0;
        return Math.Max(byType, bySource);
    }

    static Dictionary<TKey, IEnumerable<SeededEventEntry>> EffectiveGrouping<TKey>(IDictionary<TKey, IEnumerable<SeededEventEntry>> stored)
        where TKey : notnull => stored.ToDictionary(
            _ => _.Key,
            _ => (IEnumerable<SeededEventEntry>)_.Value.GroupBy(entry => entry, SeededEventEntryIdentity.Comparer)
                .SelectMany(identity => identity.GroupBy(entry => (entry.Origin, entry.Owner)).MaxBy(group => group.Count())!)
                .ToArray());

    static Dictionary<TKey, IEnumerable<SeededEventEntry>> AddGrouping<TKey>(
        IDictionary<TKey, IEnumerable<SeededEventEntry>> current,
        IEnumerable<IGrouping<TKey, SeededEventEntry>> added,
        string owner)
        where TKey : notnull
    {
        var updated = current.ToDictionary(_ => _.Key, _ => _.Value);
        foreach (var group in added)
        {
            var previous = updated.GetValueOrDefault(group.Key, []);
            var owned = previous.Where(entry => entry.Origin == SeedOrigin.Added && entry.Owner == owner)
                .ToLookup(entry => entry, SeededEventEntryIdentity.Comparer);
            var additions = group.GroupBy(entry => entry, SeededEventEntryIdentity.Comparer)
                .SelectMany(identity => identity.Skip(owned[identity.Key].Count()));
            updated[group.Key] = [.. previous, .. additions];
        }

        return updated;
    }

    static Dictionary<TKey, IEnumerable<SeededEventEntry>> ReplaceGrouping<TKey>(
        IDictionary<TKey, IEnumerable<SeededEventEntry>> current,
        IEnumerable<IGrouping<TKey, SeededEventEntry>> declared,
        string owner)
        where TKey : notnull
    {
        var retained = current.ToDictionary(
            _ => _.Key,
            _ => (IEnumerable<SeededEventEntry>)_.Value.Where(entry =>
                entry.Origin != SeedOrigin.Unknown &&
                !(entry.Origin == SeedOrigin.Declared && entry.Owner == owner)).ToArray());
        foreach (var group in declared)
        {
            retained.TryGetValue(group.Key, out var previous);
            retained[group.Key] = [.. previous ?? [], .. group];
        }

        return retained.Where(_ => _.Value.Any()).ToDictionary(_ => _.Key, _ => _.Value);
    }
}
