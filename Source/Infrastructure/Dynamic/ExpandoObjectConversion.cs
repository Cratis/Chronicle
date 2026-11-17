// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;
using System.Dynamic;
using Cratis.Concepts;
using Cratis.Reflection;

namespace Cratis.Chronicle.Dynamic;

/// <summary>
/// Conversion of property values to the values held by an <see cref="ExpandoObject"/>.
/// </summary>
internal static class ExpandoObjectConversion
{
    static readonly HashSet<Type> _valueTypes =
    [
        typeof(string),
        typeof(Guid),
        typeof(decimal),
        typeof(DateOnly),
        typeof(DateTime),
        typeof(DateTimeOffset),
        typeof(TimeOnly),
        typeof(TimeSpan),
        typeof(byte[])
    ];

    /// <summary>
    /// Get the value to store in an <see cref="ExpandoObject"/> for a property value.
    /// </summary>
    /// <param name="value">The property value.</param>
    /// <param name="camelCaseProperties">Whether or not to camel case the properties of converted objects.</param>
    /// <returns>The value to store.</returns>
    internal static object GetActualValueFrom(object value, bool camelCaseProperties = false)
    {
        var valueType = value.GetType();

        // A dictionary is also an IEnumerable<KeyValuePair<,>>, so it must be converted key-by-key before
        // the generic enumerable branch below gets a chance at it - that branch would otherwise iterate
        // its KeyValuePair<,> entries as plain elements, turning the dictionary into a list of
        // { Key, Value } objects instead of preserving it as a dictionary.
        if (valueType.IsDictionary()) return ConvertDictionaryValue(value, camelCaseProperties);
        if (IsValueType(valueType)) return value;
        if (value is IEnumerable enumerableValue) return ConvertEnumerableValue(enumerableValue, camelCaseProperties);

        return value.AsExpandoObject(camelCaseProperties);
    }

    static bool IsValueType(Type valueType) =>
        valueType.IsPrimitive ||
        valueType.IsEnum ||
        _valueTypes.Contains(valueType) ||
        valueType.IsConcept();

    static object[] ConvertEnumerableValue(IEnumerable enumerableValue, bool camelCaseProperties)
    {
        var list = new List<object>();
        foreach (var element in enumerableValue)
        {
            list.Add(GetActualValueFrom(element, camelCaseProperties));
        }

        return list.ToArray();
    }

    static Dictionary<object, object> ConvertDictionaryValue(object value, bool camelCaseProperties)
    {
        var result = new Dictionary<object, object>();
        foreach (var entry in ((IEnumerable)value).GetKeyValuePairs())
        {
            result[entry.Key] = GetActualValueFrom(entry.Value, camelCaseProperties);
        }

        return result;
    }
}
