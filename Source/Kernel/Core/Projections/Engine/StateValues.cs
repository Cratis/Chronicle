// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Dynamic;
using Cratis.Chronicle.Dynamic;

namespace Cratis.Chronicle.Projections.Engine;

/// <summary>
/// Copies values before they enter the state of one read model instance.
/// </summary>
/// <remarks>
/// The initial model state of a projection and the content of an appended event are shared - every partition of a
/// projection, and every observer of an event, sees the same instances. A complex value taken from either must not
/// become part of one instance's state or changes as-is: the pipeline and the sink write into that state in place,
/// and an <see cref="ExpandoObject"/> being written to while another partition enumerates it fails with
/// "Collection was modified".
/// </remarks>
internal static class StateValues
{
    const string ValueKey = "value";

    /// <summary>
    /// Get a copy of a value that shares no mutable instance with the original.
    /// </summary>
    /// <param name="value">Value to copy.</param>
    /// <returns>A deep copy of an <see cref="ExpandoObject"/> or a collection; values that are neither <see cref="ExpandoObject"/> nor collections are returned as-is.</returns>
    [return: NotNullIfNotNull(nameof(value))]
    public static object? Copy(object? value) => value switch
    {
        null => null,
        string => value,
        ExpandoObject expandoObject => expandoObject.Clone(),
        IEnumerable => TryCopyList(value, out var copy) ? copy : CopyThroughHolder(value),
        _ => value
    };

    /// <summary>
    /// Copies a list or object array of nulls, strings and expando objects item by item, giving exactly the <see cref="List{T}"/> of <see cref="object"/>
    /// that the generic clone builds, without the holder and the reflection around it.
    /// </summary>
    /// <param name="value">The collection to copy.</param>
    /// <param name="copy">The copy, when the collection has a shape this method handles.</param>
    /// <returns>True when copied; false for anything else (other element types, dictionaries, typed collections), which the clone owns the rules for.</returns>
    static bool TryCopyList(object value, [NotNullWhen(true)] out object? copy)
    {
        copy = null;
        if (value is List<object> list)
        {
            var result = new List<object>(list.Count);
            foreach (var item in list)
            {
                if (!TryCopyItem(item, out var itemCopy))
                {
                    return false;
                }

                result.Add(itemCopy!);
            }

            copy = result;
            return true;
        }

        if (value.GetType() == typeof(object[]))
        {
            var array = (object[])value;
            var result = new List<object>(array.Length);
            foreach (var item in array)
            {
                if (!TryCopyItem(item, out var itemCopy))
                {
                    return false;
                }

                result.Add(itemCopy!);
            }

            copy = result;
            return true;
        }

        return false;
    }

    /// <summary>
    /// Copies one list item when it is null, a string or an expando object.
    /// </summary>
    /// <param name="item">The item.</param>
    /// <param name="copy">The copied item.</param>
    /// <returns>True when the item has a handled shape.</returns>
    static bool TryCopyItem(object? item, out object? copy)
    {
        switch (item)
        {
            case null:
            case string:
                copy = item;
                return true;
            case ExpandoObject expandoObject:
                copy = expandoObject.Clone();
                return true;
            default:
                copy = null;
                return false;
        }
    }

    static object? CopyThroughHolder(object value)
    {
        var holder = new ExpandoObject();
        ((IDictionary<string, object?>)holder)[ValueKey] = value;
        return ((IDictionary<string, object?>)holder.Clone())[ValueKey];
    }
}
