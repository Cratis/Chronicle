// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Cratis.Chronicle.Concepts.SequenceQueries;
using Cratis.Chronicle.Storage.SequenceQueries;

namespace Cratis.Chronicle.Storage.InMemory.SequenceQueries;

/// <summary>
/// Applies changes to a dictionary of saved items only while they are still what the caller checked.
/// </summary>
static class ConditionalWrite
{
    /// <summary>
    /// Add an item unless one is already stored under the key.
    /// </summary>
    /// <typeparam name="TItem">The type of item stored.</typeparam>
    /// <param name="items">The stored items.</param>
    /// <param name="key">The key of the item.</param>
    /// <param name="item">The item to add.</param>
    /// <returns>The outcome of the write.</returns>
    public static SequenceQueryWriteOutcome Insert<TItem>(ConcurrentDictionary<string, TItem> items, string key, TItem item) =>
        items.TryAdd(key, item) ? SequenceQueryWriteOutcome.Applied : SequenceQueryWriteOutcome.Conflict;

    /// <summary>
    /// Replace an item, but only while it is still stored under the expected owner.
    /// </summary>
    /// <typeparam name="TItem">The type of item stored.</typeparam>
    /// <param name="items">The stored items.</param>
    /// <param name="key">The key of the item.</param>
    /// <param name="expectedOwner">The owner the item must be stored under, compared exactly.</param>
    /// <param name="ownerOf">Gets the owner an item is stored under.</param>
    /// <param name="replacement">The item to store.</param>
    /// <returns>The outcome of the write.</returns>
    public static SequenceQueryWriteOutcome Update<TItem>(
        ConcurrentDictionary<string, TItem> items,
        string key,
        SequenceQueryOwner expectedOwner,
        Func<TItem, SequenceQueryOwner> ownerOf,
        TItem replacement)
    {
        while (items.TryGetValue(key, out var current))
        {
            if (!IsOwnedBy(ownerOf(current), expectedOwner)) return SequenceQueryWriteOutcome.Conflict;
            if (items.TryUpdate(key, replacement, current)) return SequenceQueryWriteOutcome.Applied;
        }

        return SequenceQueryWriteOutcome.Conflict;
    }

    /// <summary>
    /// Remove an item, but only while it is still stored under the expected owner.
    /// </summary>
    /// <typeparam name="TItem">The type of item stored.</typeparam>
    /// <param name="items">The stored items.</param>
    /// <param name="key">The key of the item.</param>
    /// <param name="expectedOwner">The owner the item must be stored under, compared exactly.</param>
    /// <param name="ownerOf">Gets the owner an item is stored under.</param>
    /// <returns>The outcome of the write - applied also when the item is already gone.</returns>
    public static SequenceQueryWriteOutcome Delete<TItem>(
        ConcurrentDictionary<string, TItem> items,
        string key,
        SequenceQueryOwner expectedOwner,
        Func<TItem, SequenceQueryOwner> ownerOf)
    {
        while (items.TryGetValue(key, out var current))
        {
            if (!IsOwnedBy(ownerOf(current), expectedOwner)) return SequenceQueryWriteOutcome.Conflict;
            if (items.TryRemove(new KeyValuePair<string, TItem>(key, current))) return SequenceQueryWriteOutcome.Applied;
        }

        return SequenceQueryWriteOutcome.Applied;
    }

    static bool IsOwnedBy(SequenceQueryOwner stored, SequenceQueryOwner expected) =>
        string.Equals(stored.Value, expected.Value, StringComparison.Ordinal);
}
