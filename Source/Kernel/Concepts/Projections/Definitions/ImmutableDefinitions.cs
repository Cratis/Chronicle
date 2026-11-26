// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.ObjectModel;

namespace Cratis.Chronicle.Concepts.Projections.Definitions;

/// <summary>
/// Takes the snapshots of collections that keep a definition from changing after it was created.
/// </summary>
/// <remarks>
/// A definition is passed by reference between grains of a silo. Whoever built it may still hold the collection it was built from, so the definition takes
/// its own copy that nobody else holds. Enumeration order is kept, because the stored form of a definition and its digest depend on it. The snapshots are
/// the BCL read-only collections, which the Orleans serializers know.
/// </remarks>
public static class ImmutableDefinitions
{
    /// <summary>
    /// Take a snapshot of a dictionary that can not be changed.
    /// </summary>
    /// <param name="source">The dictionary to take a snapshot of.</param>
    /// <typeparam name="TKey">Type of key.</typeparam>
    /// <typeparam name="TValue">Type of value.</typeparam>
    /// <returns>The snapshot. A definition that was left uninitialized by storage holds null, which stays null.</returns>
    public static IReadOnlyDictionary<TKey, TValue> Snapshot<TKey, TValue>(IEnumerable<KeyValuePair<TKey, TValue>> source)
        where TKey : notnull =>
        source is null ? null! : new ReadOnlyDictionary<TKey, TValue>(new Dictionary<TKey, TValue>(source));

    /// <summary>
    /// Take a snapshot of an optional dictionary that can not be changed.
    /// </summary>
    /// <param name="source">The dictionary to take a snapshot of.</param>
    /// <typeparam name="TKey">Type of key.</typeparam>
    /// <typeparam name="TValue">Type of value.</typeparam>
    /// <returns>The snapshot, or null when there is nothing to take a snapshot of.</returns>
    public static IReadOnlyDictionary<TKey, TValue>? SnapshotOrNull<TKey, TValue>(IEnumerable<KeyValuePair<TKey, TValue>>? source)
        where TKey : notnull =>
        source is null ? null : Snapshot(source);

    /// <summary>
    /// Take a snapshot of a sequence that can not be changed.
    /// </summary>
    /// <param name="source">The sequence to take a snapshot of.</param>
    /// <typeparam name="T">Type of item.</typeparam>
    /// <returns>The snapshot. A definition that was left uninitialized by storage holds null, which stays null.</returns>
    public static IEnumerable<T> Snapshot<T>(IEnumerable<T> source) => source is null ? null! : new ReadOnlyCollection<T>([.. source]);

    /// <summary>
    /// Take a snapshot of an optional sequence that can not be changed.
    /// </summary>
    /// <param name="source">The sequence to take a snapshot of.</param>
    /// <typeparam name="T">Type of item.</typeparam>
    /// <returns>The snapshot, or null when there is nothing to take a snapshot of.</returns>
    public static IEnumerable<T>? SnapshotOrNull<T>(IEnumerable<T>? source) => source is null ? null : Snapshot(source);
}
