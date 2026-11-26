// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;

namespace Cratis.Chronicle.Testing.Assertions;

/// <summary>
/// Reads mutable and read-only dictionaries independently of enumeration order.
/// </summary>
internal static class ExpectedDictionary
{
    /// <summary>
    /// Reads entries from mutable or read-only dictionary interfaces.
    /// </summary>
    /// <param name="value">The potential dictionary.</param>
    /// <returns>The entries, or null for a non-dictionary.</returns>
    internal static IReadOnlyList<DictionaryEntry>? Entries(object value)
    {
        if (value is IDictionary dictionary)
        {
            return dictionary.Keys.Cast<object>().Select(key => new DictionaryEntry(key, dictionary[key])).ToArray();
        }
        if (!value.GetType().GetInterfaces().Any(IsDictionary)) return null;

        return ((IEnumerable)value).Cast<object>().Select(Entry).ToArray();
    }

    static bool IsDictionary(Type type) => type.IsGenericType &&
        (type.GetGenericTypeDefinition() == typeof(IDictionary<,>) || type.GetGenericTypeDefinition() == typeof(IReadOnlyDictionary<,>));

    static DictionaryEntry Entry(object entry) => new(
        entry.GetType().GetProperty("Key")!.GetValue(entry)!,
        entry.GetType().GetProperty("Value")!.GetValue(entry));
}
