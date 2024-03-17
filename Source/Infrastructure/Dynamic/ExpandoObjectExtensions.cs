// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;
using System.Dynamic;
using Cratis.Concepts;
using Cratis.Objects;
using Cratis.Properties;
using Cratis.Reflection;
using Cratis.Strings;

namespace Cratis.Dynamic;

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

        foreach (var (key, value) in originalAsDictionary)
        {
            if (value is null)
            {
                cloneAsDictionary.Add(key, null!);
                continue;
            }

            var valueType = value.GetType();
            if (!valueType.IsPrimitive &&
                valueType != typeof(ExpandoObject) &&
                valueType != typeof(string) &&
                valueType != typeof(Guid) &&
                !valueType.IsConcept() &&
                value is IEnumerable enumerableValue)
            {
                var elementType = valueType.GetEnumerableElementType();
                var list = (Activator.CreateInstance(typeof(List<>).MakeGenericType(elementType)) as IList)!;
                foreach (var element in enumerableValue)
                {
                    list.Add(element is ExpandoObject child ? child.Clone() : element.Clone());
                }
                cloneAsDictionary.Add(key, list);
            }
            else
            {
                cloneAsDictionary.Add(key, value is ExpandoObject nested ? nested.Clone() : value.Clone());
            }
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
                value = GetActualValueFrom(value);
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
            var rightValue = value;
            if (resultAsDictionary.ContainsKey(key) && resultAsDictionary[key] is ExpandoObject leftValueExpandoObject &&
                value is ExpandoObject valueAsExpandoObject)
            {
                rightValue = leftValueExpandoObject.MergeWith(valueAsExpandoObject);
            }
            else if (rightValue is ExpandoObject rightValueAsExpandoObject)
            {
                rightValue = rightValueAsExpandoObject.Clone();
            }

            resultAsDictionary[key] = rightValue;
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
    /// <exception cref="SegmentValueIsNotCollection">Thrown if a segment value should be expando object.</exception>
    public static ExpandoObject EnsurePath(this ExpandoObject target, PropertyPath property, ArrayIndexers arrayIndexers)
    {
        var currentTarget = target as IDictionary<string, object>;
        var segments = property.Segments.ToArray();
        var currentPath = new PropertyPath(string.Empty);

        for (var propertyIndex = 0; propertyIndex < segments.Length - 1; propertyIndex++)
        {
            var segment = segments[propertyIndex];
            currentPath += segment;
            switch (segment)
            {
                case PropertyName propertyName:
                    {
                        if (!currentTarget.ContainsKey(propertyName.Value))
                        {
                            var nested = new ExpandoObject();
                            currentTarget[segment.Value] = nested;
                            currentTarget = nested!;
                        }
                        else
                        {
                            currentTarget = ((ExpandoObject)currentTarget[segment.Value])!;
                        }
                    }
                    break;

                case ArrayProperty arrayProperty:
                    {
                        var indexer = arrayIndexers.GetFor(currentPath);
                        IEnumerable<ExpandoObject> collection;
                        if (!currentTarget.ContainsKey(arrayProperty.Value))
                        {
                            collection = new List<ExpandoObject>();
                            currentTarget[segment.Value] = collection;
                        }
                        else
                        {
                            if (currentTarget[segment.Value] is not IEnumerable enumerable)
                            {
                                throw new SegmentValueIsNotCollection(property, segment);
                            }
                            collection = ((IEnumerable)currentTarget[segment.Value]).OfType<ExpandoObject>().ToList();
                        }

                        IDictionary<string, object>? element = null;

                        if (!indexer.IdentifierProperty.IsSet &&
                            indexer.Identifier is int index &&
                            collection.Count() > index)
                        {
                            element = collection.ToArray()[index]!;
                        }
                        else
                        {
                            element = collection
                                .Cast<IDictionary<string, object>>()
                                .SingleOrDefault(item =>
                                    item.ContainsKey(indexer.IdentifierProperty.Path) &&
                                    item[indexer.IdentifierProperty.Path].IsEqualTo(indexer.Identifier));
                        }

                        if (element == default)
                        {
                            element = new ExpandoObject()!;
                            if (indexer.IdentifierProperty.IsSet)
                            {
                                element[indexer.IdentifierProperty.Path] = indexer.Identifier;
                            }
                            currentTarget[segment.Value] = collection.Append((element as ExpandoObject)!).ToList();
                        }
                        currentTarget = (element as ExpandoObject)!;
                    }
                    break;
            }
        }

        return (currentTarget as ExpandoObject)!;
    }

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
        if (!inner.ContainsKey(childrenProperty.LastSegment.Value) || inner[childrenProperty.LastSegment.Value] is null)
        {
            inner[childrenProperty.LastSegment.Value] = new List<TChild>();
        }

        if (!(inner[childrenProperty.LastSegment.Value] is IEnumerable))
        {
            throw new ChildrenPropertyIsNotEnumerable(childrenProperty);
        }

        var items = (inner[childrenProperty.LastSegment.Value] as IEnumerable)!.Cast<TChild>();
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
        items!.Any((IDictionary<string, object> _) => _.ContainsKey(identityProperty.Path) && _[identityProperty.Path].Equals(key));

    /// <summary>
    /// Get an item with a specific key in a collection of <see cref="ExpandoObject"/> items.
    /// </summary>
    /// <param name="items">Items to check.</param>
    /// <param name="identityProperty"><see cref="PropertyPath"/> holding identity on each item.</param>
    /// <param name="key">The key value to check for.</param>
    /// <returns>The item or null if not found.</returns>
    public static ExpandoObject? GetItem(this IEnumerable<ExpandoObject> items, PropertyPath identityProperty, object key)
    {
        var item = items!.SingleOrDefault((IDictionary<string, object> _) => _.ContainsKey(identityProperty.Path) && _[identityProperty.Path].Equals(key));
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

    static object GetActualValueFrom(object value)
    {
        var valueType = value.GetType();
        if (!valueType.IsPrimitive &&
            valueType != typeof(string) &&
            valueType != typeof(Guid) &&
            valueType != typeof(DateOnly) &&
            valueType != typeof(DateTime) &&
            valueType != typeof(DateTimeOffset) &&
            valueType != typeof(TimeOnly) &&
            !valueType.IsConcept())
        {
            if (value is IEnumerable enumerableValue)
            {
                var list = new List<object>();

                foreach (var element in enumerableValue)
                {
                    list.Add(GetActualValueFrom(element));
                }

                value = list.ToArray();
            }
            else
            {
                value = value.AsExpandoObject();
            }
        }

        return value;
    }
}
