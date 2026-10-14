// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Seeding;

namespace Cratis.Chronicle.Seeding;

/// <summary>
/// Replaces a program's declaration while retaining explicit additions and other programs' declarations.
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
