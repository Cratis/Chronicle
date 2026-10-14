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
    /// Uses the maximum identity count across provenance groups, retaining stored entry order.
    /// </summary>
    /// <param name="stored">The set containing all provenance groups.</param>
    /// <returns>The effective seed multiset for a namespace.</returns>
    internal static EventSeeds Effective(EventSeeds stored) => new(
        EffectiveGrouping(stored.ByEventType),
        EffectiveGrouping(stored.ByEventSource));

    /// <summary>
    /// Offers the full effective sequence for each event source whose count grew, so namespace count-based
    /// idempotency appends missing occurrences in order and safely retries partial dispatches.
    /// </summary>
    /// <param name="before">The stored set before registration.</param>
    /// <param name="after">The proposed stored set after registration.</param>
    /// <returns>The entries to offer to existing namespaces.</returns>
    internal static IEnumerable<SeededEventEntry> GrowingEntries(EventSeeds before, EventSeeds after)
    {
        var previous = Effective(before);
        return Effective(after).ByEventSource.Values
            .Where(entries => entries.GroupBy(entry => entry, SeededEventEntryIdentity.Comparer)
                .Any(group => group.Count() > Count(previous, group.Key)))
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
            _ => (IEnumerable<SeededEventEntry>)EffectiveEntries(_.Value));

    static SeededEventEntry[] EffectiveEntries(IEnumerable<SeededEventEntry> stored)
    {
        var entries = stored.ToArray();
        var remaining = entries.GroupBy(entry => entry, SeededEventEntryIdentity.Comparer)
            .ToDictionary(
                identity => identity.Key,
                identity => identity.GroupBy(entry => (entry.Origin, entry.Owner)).Max(group => group.Count()),
                SeededEventEntryIdentity.Comparer);
        var effective = new List<SeededEventEntry>();
        foreach (var entry in entries)
        {
            if (remaining[entry] > 0)
            {
                effective.Add(entry);
                remaining[entry]--;
            }
        }

        return [.. effective];
    }

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
                .GroupBy(entry => entry, SeededEventEntryIdentity.Comparer)
                .ToDictionary(identity => identity.Key, identity => identity.Count(), SeededEventEntryIdentity.Comparer);
            updated[group.Key] = [.. previous, .. MissingAdditions(group, owned)];
        }

        return updated;
    }

    static IEnumerable<SeededEventEntry> MissingAdditions(
        IEnumerable<SeededEventEntry> offered,
        Dictionary<SeededEventEntry, int> owned)
    {
        foreach (var entry in offered)
        {
            var remaining = owned.GetValueOrDefault(entry);
            if (remaining > 0)
            {
                owned[entry] = remaining - 1;
            }
            else
            {
                yield return entry;
            }
        }
    }

    static Dictionary<TKey, IEnumerable<SeededEventEntry>> ReplaceGrouping<TKey>(
        IDictionary<TKey, IEnumerable<SeededEventEntry>> current,
        IEnumerable<IGrouping<TKey, SeededEventEntry>> declared,
        string owner)
        where TKey : notnull
    {
        var declarations = declared.ToDictionary(_ => _.Key, _ => _.ToArray());
        var updated = current.ToDictionary(
            _ => _.Key,
            _ => (IEnumerable<SeededEventEntry>)ReplaceEntries(_.Value, declarations.GetValueOrDefault(_.Key, []), owner));
        foreach (var declaration in declarations)
        {
            updated.TryAdd(declaration.Key, declaration.Value);
        }

        return updated.Where(_ => _.Value.Any()).ToDictionary(_ => _.Key, _ => _.Value);
    }

    static SeededEventEntry[] ReplaceEntries(IEnumerable<SeededEventEntry> current, SeededEventEntry[] declared, string owner)
    {
        var updated = new List<SeededEventEntry>();
        var replaced = false;
        foreach (var entry in current)
        {
            if (entry.Origin == SeedOrigin.Declared && entry.Owner == owner)
            {
                if (!replaced)
                {
                    updated.AddRange(declared);
                }

                replaced = true;
            }
            else if (entry.Origin != SeedOrigin.Unknown)
            {
                updated.Add(entry);
            }
        }

        if (!replaced)
        {
            updated.AddRange(declared);
        }

        return [.. updated];
    }
}
