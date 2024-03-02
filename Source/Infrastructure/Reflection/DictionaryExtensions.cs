// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;

namespace Aksio.Cratis.Reflection;

/// <summary>
/// Provides extension methods for <see cref="Type"/>.
/// </summary>
public static class DictionaryExtensions
{
    /// <summary>
    /// Check if a type is a dictionary.
    /// </summary>
    /// <param name="type">Type to check.</param>
    /// <returns>True if it is, false if not.</returns>
    public static bool IsDictionary(this Type type) =>
        (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IDictionary<,>)) ||
        type.GetInterfaces().Any(_ => _.IsGenericType && _.GetGenericTypeDefinition() == typeof(IDictionary<,>));

    /// <summary>
    /// Get the key type of a dictionary.
    /// </summary>
    /// <param name="type">Dictionary type to get from.</param>
    /// <returns>Type of key.</returns>
    public static Type GetKeyType(this Type type)
    {
        var dictionaryInterface = Array.Find(type.GetInterfaces(), _ => _.IsGenericType && _.GetGenericTypeDefinition() == typeof(IDictionary<,>));
        if (dictionaryInterface is null) return typeof(object);

        return dictionaryInterface.GetGenericArguments()[0];
    }

    /// <summary>
    /// Get the key type of a dictionary.
    /// </summary>
    /// <param name="type">Dictionary type to get from.</param>
    /// <returns>Type of key.</returns>
    public static Type GetValueType(this Type type)
    {
        var dictionaryInterface = Array.Find(type.GetInterfaces(), _ => _.IsGenericType && _.GetGenericTypeDefinition() == typeof(IDictionary<,>));
        if (dictionaryInterface is null) return typeof(object);

        return dictionaryInterface.GetGenericArguments()[1];
    }

    /// <summary>
    /// Get the key value pairs from a dictionary.
    /// </summary>
    /// <param name="enumerable"><see cref="IEnumerable"/> which is a dictionary to get from.</param>
    /// <returns>A collection of key value pairs of string type.</returns>
    public static IEnumerable<KeyValuePair<string, object>> GetKeyValuePairs(this IEnumerable enumerable)
    {
        var dictionaryType = enumerable.GetType();
        if (!dictionaryType.IsDictionary())
        {
            return Enumerable.Empty<KeyValuePair<string, object>>();
        }

        var keyType = dictionaryType.GetKeyType();
        var valueType = dictionaryType.GetValueType();
        var keyValuePairType = typeof(KeyValuePair<,>).MakeGenericType(keyType, valueType);
        var keyProperty = keyValuePairType.GetProperty(nameof(KeyValuePair<object, object>.Key))!;
        var valueProperty = keyValuePairType.GetProperty(nameof(KeyValuePair<object, object>.Value))!;
        var keyValuePairs = new List<KeyValuePair<string, object>>();

        foreach (var keyValuePair in enumerable)
        {
            keyValuePairs.Add(new KeyValuePair<string, object>(
                keyProperty.GetValue(keyValuePair)?.ToString() ?? string.Empty,
                valueProperty.GetValue(keyValuePair)!));
        }

        return keyValuePairs;
    }
}
