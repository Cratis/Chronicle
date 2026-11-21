// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;
using System.Dynamic;
using Cratis.Chronicle.Properties;
using Cratis.Strings;

namespace Cratis.Chronicle.Dynamic;

/// <summary>
/// Extension methods for working with <see cref="ExpandoObject"/>.
/// </summary>
public static class ExpandoObjectExtensions
{
    /// <summary>
    /// Deep clone an <see cref="ExpandoObject"/>.
    /// </summary>
    /// <param name="original">The original <see cref="ExpandoObject"/>.</param>
    /// <returns>A cloned <see cref="ExpandoObject"/>.</returns>
    /// <remarks>
    /// If any of the values represents complex objects, it will not clone these
    /// and create new fresh instances - it will just copy these across.
    /// </remarks>
    public static ExpandoObject Clone(this ExpandoObject original)
    {
        var clone = new ExpandoObject();
        var originalAsDictionary = original as IDictionary<string, object>;
        var cloneAsDictionary = clone as IDictionary<string, object>;

        foreach (var (key, value) in ExpandoObjectCloning.Snapshot(originalAsDictionary))
        {
            cloneAsDictionary.Add(key, ExpandoObjectCloning.CloneValue(value)!);
        }

        return clone;
    }

    /// <summary>
    /// Converts an object to a dynamic <see cref="ExpandoObject"/>.
    /// </summary>
    /// <param name="original">Original object to convert.</param>
    /// <param name="camelCaseProperties">Whether or not to camel case the properties.</param>
    /// <returns>A new <see cref="ExpandoObject"/> representing the given object.</returns>
    public static ExpandoObject AsExpandoObject(this object original, bool camelCaseProperties = false)
    {
        if (original is ExpandoObject) return (original as ExpandoObject)!;

        var expando = new ExpandoObject();
        var expandoAsDictionary = expando as IDictionary<string, object>;

        foreach (var property in original.GetType().GetProperties())
        {
            var propertyName = camelCaseProperties ? property.Name.ToCamelCase() : property.Name;
            var value = property.GetValue(original, null);
            if (value != null)
            {
                value = ExpandoObjectConversion.GetActualValueFrom(value, camelCaseProperties);
            }
            expandoAsDictionary[propertyName] = value!;
        }

        return expando;
    }

    /// <summary>
    /// Creates a clone of left and applies all content from right on top, overwriting any existing properties in left.
    /// </summary>
    /// <param name="left">The left <see cref="ExpandoObject"/>.</param>
    /// <param name="right">The right <see cref="ExpandoObject"/>.</param>
    /// <returns>A new <see cref="ExpandoObject"/>.</returns>
    public static ExpandoObject MergeWith(this ExpandoObject left, ExpandoObject right)
    {
        var result = left.Clone();
        var resultAsDictionary = result as IDictionary<string, object>;
        var rightAsDictionary = right as IDictionary<string, object>;

        foreach (var (key, value) in rightAsDictionary)
        {
            resultAsDictionary[key] = MergeValue(resultAsDictionary, key, value);
        }

        return result;
    }

    /// <summary>
    /// Ensure a specific path for a <see cref="PropertyPath"/> exists on an <see cref="ExpandoObject"/>..
    /// </summary>
    /// <param name="target">Target <see cref="ExpandoObject"/>.</param>
    /// <param name="property"><see cref="PropertyPath"/> to get or create for.</param>
    /// <param name="arrayIndexers">All <see cref="ArrayIndexer">array indexers</see>.</param>
    /// <returns><see cref="ExpandoObject"/> at property.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the current target unexpectedly becomes null while traversing array properties.</exception>
    /// <exception cref="SegmentValueIsNotCollection">Thrown if a segment value should be expando object.</exception>
    public static ExpandoObject EnsurePath(this ExpandoObject target, PropertyPath property, ArrayIndexers arrayIndexers) =>
        ExpandoObjectPaths.EnsurePath(target, property, arrayIndexers);

    /// <summary>
    /// Ensures that a collection exists for a specific <see cref="PropertyPath"/>.
    /// </summary>
    /// <typeparam name="TChild">Type of child for the collection.</typeparam>
    /// <param name="target">Target <see cref="ExpandoObject"/>.</param>
    /// <param name="childrenProperty"><see cref="PropertyPath"/> to ensure collection for.</param>
    /// <param name="arrayIndexers">Any <see cref="ArrayIndexer">array indexers</see>.</param>
    /// <returns>The ensured <see cref="ICollection{ExpandoObject}"/>.</returns>
    /// <exception cref="ChildrenPropertyIsNotEnumerable">Thrown if there is an existing property and it is not enumerable.</exception>
    public static ICollection<TChild> EnsureCollection<TChild>(this ExpandoObject target, PropertyPath childrenProperty, ArrayIndexers arrayIndexers)
    {
        var inner = target.EnsurePath(childrenProperty, arrayIndexers) as IDictionary<string, object>;
        if (!inner.TryGetValue(childrenProperty.LastSegment.Value, out var value) || value is null)
        {
            value = new List<TChild>();
            inner[childrenProperty.LastSegment.Value] = value;
        }

        if (!(value is IEnumerable enumerable))
        {
            throw new ChildrenPropertyIsNotEnumerable(childrenProperty);
        }

        var items = enumerable.Cast<TChild>();
        if (items is not IList<TChild>)
        {
            items = new List<TChild>(items!);
        }
        inner[childrenProperty.LastSegment.Value] = items;
        return (items as ICollection<TChild>)!;
    }

    /// <summary>
    /// Check if there is an item with a specific key in a collection of <see cref="ExpandoObject"/> items.
    /// </summary>
    /// <param name="items">Items to check.</param>
    /// <param name="identityProperty"><see cref="PropertyPath"/> holding identity on each item.</param>
    /// <param name="key">The key value to check for.</param>
    /// <returns>True if there is an item, false if not.</returns>
    public static bool Contains(this IEnumerable<ExpandoObject> items, PropertyPath identityProperty, object key) =>
        items!.Any((IDictionary<string, object> item) => HasIdentity(item, identityProperty, key));

    /// <summary>
    /// Get an item with a specific key in a collection of <see cref="ExpandoObject"/> items.
    /// </summary>
    /// <param name="items">Items to check.</param>
    /// <param name="identityProperty"><see cref="PropertyPath"/> holding identity on each item.</param>
    /// <param name="key">The key value to check for.</param>
    /// <returns>The item or null if not found.</returns>
    public static ExpandoObject? GetItem(this IEnumerable<ExpandoObject> items, PropertyPath identityProperty, object key)
    {
        var item = items!.SingleOrDefault((IDictionary<string, object> candidate) => HasIdentity(candidate, identityProperty, key));
        return item is not null ? item as ExpandoObject : null;
    }

    /// <summary>
    /// Remove any null values from an <see cref="ExpandoObject"/>.
    /// </summary>
    /// <param name="target">The <see cref="ExpandoObject"/> to remove from.</param>
    public static void RemoveNulls(this ExpandoObject target)
    {
        var targetAsDictionary = target as IDictionary<string, object>;
        foreach (var (key, value) in targetAsDictionary.ToArray())
        {
            if (value is null)
            {
                targetAsDictionary.Remove(key);
            }
            else if (value is ExpandoObject nested)
            {
                nested.RemoveNulls();
            }
        }
    }

    static object MergeValue(IDictionary<string, object> result, string key, object value)
    {
        if (value is not ExpandoObject rightExpando) return value;

        return result.TryGetValue(key, out var leftValue) && leftValue is ExpandoObject leftExpando
            ? leftExpando.MergeWith(rightExpando)
            : rightExpando.Clone();
    }

    static bool HasIdentity(IDictionary<string, object> item, PropertyPath identityProperty, object key) =>
        ChildIdentity.TryGetValue(item, identityProperty, out var itemValue) &&
        itemValue is not null &&
        KeyValueEquality.AreEqual(itemValue, key);
}
