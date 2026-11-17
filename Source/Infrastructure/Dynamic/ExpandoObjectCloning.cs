// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;
using System.Dynamic;
using Cratis.Chronicle.Objects;
using Cratis.Concepts;
using Cratis.Reflection;

namespace Cratis.Chronicle.Dynamic;

/// <summary>
/// Deep cloning of the values held by an <see cref="ExpandoObject"/>.
/// </summary>
internal static class ExpandoObjectCloning
{
    /// <summary>
    /// Takes a snapshot of the key-value pairs of an <see cref="ExpandoObject"/>.
    /// </summary>
    /// <param name="original">The <see cref="ExpandoObject"/> as a dictionary.</param>
    /// <returns>The copied key-value pairs.</returns>
    /// <remarks>
    /// Since the internals of <see cref="ExpandoObject"/> are not necessarily thread safe, we need to reduce the risk of conflicts
    /// when copying the key-value pairs. This is a best effort to avoid exceptions due to concurrent modifications.
    /// </remarks>
    internal static List<KeyValuePair<string, object>> Snapshot(IDictionary<string, object> original)
    {
        try
        {
            return new List<KeyValuePair<string, object>>(original);
        }
        catch (InvalidOperationException)
        {
            // Handle modification issue by retrying, could still fail - so this is a best effort fallback mechanism
            return original.ToList();
        }
    }

    /// <summary>
    /// Clone a single value held by an <see cref="ExpandoObject"/>.
    /// </summary>
    /// <param name="value">The value to clone.</param>
    /// <returns>The cloned value.</returns>
    internal static object? CloneValue(object? value)
    {
        if (value is null) return null;
        if (value is ExpandoObject nested) return nested.Clone();

        var valueType = value.GetType();

        // A dictionary is also an IEnumerable<KeyValuePair<,>>, so it must be cloned key-by-key before
        // the generic enumerable branch below gets a chance at it - that branch iterates any IEnumerable
        // into a List<T>, which for a dictionary would silently replace it with a
        // List<KeyValuePair<TKey, TValue>>, losing the dictionary shape entirely.
        if (valueType.IsDictionary()) return CloneDictionary(value, valueType);

        if (IsCloneableCollection(valueType) && value is IEnumerable enumerableValue)
        {
            return CloneCollection(enumerableValue, valueType);
        }

        return value.Clone();
    }

    static bool IsCloneableCollection(Type valueType) =>
        !valueType.IsPrimitive &&
        valueType != typeof(string) &&
        valueType != typeof(Guid) &&
        valueType != typeof(byte[]) &&
        !valueType.IsConcept();

    static IList CloneCollection(IEnumerable enumerableValue, Type valueType)
    {
        var elementType = valueType.GetEnumerableElementType();
        var list = (Activator.CreateInstance(typeof(List<>).MakeGenericType(elementType)) as IList)!;
        foreach (var element in enumerableValue)
        {
            list.Add(element is ExpandoObject child ? child.Clone() : element.Clone());
        }

        return list;
    }

    static IDictionary CloneDictionary(object value, Type dictionaryType)
    {
        var keyType = dictionaryType.GetKeyType();
        var valueType = dictionaryType.GetValueType();
        var clone = (Activator.CreateInstance(typeof(Dictionary<,>).MakeGenericType(keyType, valueType)) as IDictionary)!;

        var keyValuePairType = typeof(KeyValuePair<,>).MakeGenericType(keyType, valueType);
        var keyProperty = keyValuePairType.GetProperty(nameof(KeyValuePair<object, object>.Key))!;
        var valueProperty = keyValuePairType.GetProperty(nameof(KeyValuePair<object, object>.Value))!;

        foreach (var keyValuePair in (IEnumerable)value)
        {
            var entryKey = keyProperty.GetValue(keyValuePair)!;
            var entryValue = valueProperty.GetValue(keyValuePair);
            clone[entryKey] = entryValue is ExpandoObject nestedEntry ? nestedEntry.Clone() : entryValue?.Clone();
        }

        return clone;
    }
}
