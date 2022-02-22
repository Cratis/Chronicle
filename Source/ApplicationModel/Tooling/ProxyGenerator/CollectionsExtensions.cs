// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Applications.ProxyGenerator;

/// <summary>
/// Provides a set of extension methods for different collection and enumerable types.
/// </summary>
public static class CollectionsExtensions
{
    /// <summary>
    /// Enumerate an enumerable and call the given Action for each item.
    /// </summary>
    /// <typeparam name="T">Item type.</typeparam>
    /// <param name="enumerable"><see cref="IEnumerable{T}"/> to enumerate.</param>
    /// <param name="action"><see cref="Action{T}"/> to call for each item.</param>
    public static void ForEach<T>(this IEnumerable<T> enumerable, Action<T> action)
    {
        foreach (var item in enumerable)
            action(item);
    }

    /// <summary>
    /// Combines multiple lookups into a single lookup.
    /// </summary>
    /// <typeparam name="TKey">The type of the keys.</typeparam>
    /// <typeparam name="TElement">The type of the elements.</typeparam>
    /// <param name="lookups">A collection of lookups to combine.</param>
    /// <returns>A single lookup which takes a key into all values with this key in all incoming lookups.</returns>
    public static ILookup<TKey, TElement> Combine<TKey, TElement>(this IEnumerable<ILookup<TKey, TElement>> lookups)
    {
        return lookups
            .SelectMany(l => l)
            .SelectMany(l => l.Select(value => (l.Key, Value: value)))
            .ToLookup(x => x.Key, x => x.Value);
    }
}
