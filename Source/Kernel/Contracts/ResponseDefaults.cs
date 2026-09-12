// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Contracts;

/// <summary>
/// Creates empty payloads for query and command result envelopes at the wire boundary.
/// </summary>
internal static class ResponseDefaults
{
    /// <summary>
    /// Creates a fresh empty payload without sharing mutable collections between responses.
    /// </summary>
    /// <typeparam name="T">The payload type.</typeparam>
    /// <returns>An empty collection, empty string, value default, or initialized response object.</returns>
    internal static T Create<T>()
    {
        var type = typeof(T);
        if (type == typeof(string))
        {
            return (T)(object)string.Empty;
        }

        if (type.IsArray)
        {
            return (T)(object)Array.CreateInstance(type.GetElementType()!, new int[type.GetArrayRank()]);
        }

        if (type.IsGenericType)
        {
            var definition = type.GetGenericTypeDefinition();
            var arguments = type.GetGenericArguments();
            if (definition == typeof(IEnumerable<>) || definition == typeof(ICollection<>) ||
                definition == typeof(IList<>) || definition == typeof(IReadOnlyCollection<>) ||
                definition == typeof(IReadOnlyList<>))
            {
                return (T)Activator.CreateInstance(typeof(List<>).MakeGenericType(arguments))!;
            }

            if (definition == typeof(IDictionary<,>) || definition == typeof(IReadOnlyDictionary<,>))
            {
                return (T)Activator.CreateInstance(typeof(Dictionary<,>).MakeGenericType(arguments))!;
            }

            if (definition == typeof(ISet<>) || definition == typeof(IReadOnlySet<>))
            {
                return (T)Activator.CreateInstance(typeof(HashSet<>).MakeGenericType(arguments))!;
            }
        }

        return type.IsValueType ? default! : Activator.CreateInstance<T>();
    }
}
