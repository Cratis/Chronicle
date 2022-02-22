// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;
using System.Reflection;
using Aksio.Cratis.Reflection;

namespace Aksio.Cratis.Collections;

/// <summary>
/// Provides a set of extension methods for different collection and enumerable types.
/// </summary>
public static class CollectionExtensions
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

    /// <summary>
    /// Count number of items from an enumerable.
    /// </summary>
    /// <param name="enumerable">Enumerable to count.</param>
    /// <returns>The number of items it enumerates.</returns>
    /// <remarks>
    /// This method will look for more concrete types and leverage the fastest way to get the count without
    /// having to enumerate it.
    /// If no other options are available, it will enumerate it.
    /// </remarks>
    public static int CountElements(this IEnumerable enumerable)
    {
        var type = enumerable.GetType();
        if (type.ImplementsOpenGeneric(typeof(IEnumerable<>)))
        {
            var countMethod = typeof(Enumerable).GetMethods(BindingFlags.Public | BindingFlags.Static)
                .Single(_ => _.Name == nameof(Enumerable.Count) && _.GetParameters().Length == 1);
            var enumerableInterface = type.GetInterface(typeof(IEnumerable<>).Name);
            var genericCountMethod = countMethod.MakeGenericMethod(enumerableInterface!.GenericTypeArguments);
            return (int)genericCountMethod.Invoke(null, new object[] { enumerable })!;
        }

        var count = 0;
        foreach (var element in enumerable)
        {
            count++;
        }

        return count;
    }

    /// <summary>
    /// Converts an enumerable to an array of objects.
    /// </summary>
    /// <param name="enumerable">Enumerable to convert.</param>
    /// <returns>Array of objects.</returns>
    public static object[] ToObjectArray(this IEnumerable enumerable)
    {
        var list = new List<object>();
        foreach (var element in enumerable)
        {
            list.Add(element);
        }
        return list.ToArray();
    }
}
