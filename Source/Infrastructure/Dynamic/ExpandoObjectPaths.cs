// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;
using System.Dynamic;
using Cratis.Chronicle.Objects;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Dynamic;

/// <summary>
/// Creation of missing paths inside an <see cref="ExpandoObject"/>.
/// </summary>
internal static class ExpandoObjectPaths
{
    /// <summary>
    /// Ensure a specific path exists on an <see cref="ExpandoObject"/>.
    /// </summary>
    /// <param name="target">Target <see cref="ExpandoObject"/>.</param>
    /// <param name="property"><see cref="PropertyPath"/> to get or create for.</param>
    /// <param name="arrayIndexers">All <see cref="ArrayIndexer">array indexers</see>.</param>
    /// <returns><see cref="ExpandoObject"/> at property.</returns>
    internal static ExpandoObject EnsurePath(ExpandoObject target, PropertyPath property, ArrayIndexers arrayIndexers)
    {
        var currentTarget = target as IDictionary<string, object>;
        var segments = property.Segments.ToArray();
        var currentPath = PropertyPath.Root;

        for (var propertyIndex = 0; propertyIndex < segments.Length - 1; propertyIndex++)
        {
            var segment = segments[propertyIndex];
            currentPath += segment;
            switch (segment)
            {
                case PropertyName propertyName:
                    currentTarget = EnsureNestedObject(currentTarget, propertyName);
                    break;

                case ArrayProperty arrayProperty:
                    currentTarget = EnsureArrayElements(currentTarget, property, arrayProperty, currentPath, arrayIndexers);
                    break;
            }
        }

        return (currentTarget as ExpandoObject)!;
    }

    static IDictionary<string, object> EnsureNestedObject(IDictionary<string, object> current, PropertyName propertyName)
    {
        if (current.TryGetValue(propertyName.Value, out var nestedValue) && nestedValue is not null)
        {
            return ((ExpandoObject)nestedValue)!;
        }

        var nested = new ExpandoObject();
        current[propertyName.Value] = nested;
        return nested!;
    }

    static IDictionary<string, object> EnsureArrayElements(
        IDictionary<string, object> current,
        PropertyPath property,
        ArrayProperty arrayProperty,
        PropertyPath currentPath,
        ArrayIndexers arrayIndexers)
    {
        var matchingIndexers = arrayIndexers.All
            .Where(indexer => indexer.ArrayProperty == currentPath)
            .ToArray();

        if (matchingIndexers.Length == 0)
        {
            matchingIndexers = [arrayIndexers.GetFor(currentPath)];
        }

        foreach (var indexer in matchingIndexers)
        {
            if (current is null)
            {
                throw new InvalidOperationException("Current target cannot be null while processing array properties.");
            }

            current = EnsureArrayElement(current, property, arrayProperty, indexer);
        }

        return current;
    }

    static IDictionary<string, object> EnsureArrayElement(
        IDictionary<string, object> current,
        PropertyPath property,
        ArrayProperty arrayProperty,
        ArrayIndexer indexer)
    {
        var collection = EnsureCollection(current, property, arrayProperty);
        var element = FindElement(collection, indexer);

        if (element == default)
        {
            element = new ExpandoObject()!;
            if (indexer.IdentifierProperty.IsSet)
            {
                indexer.IdentifierProperty.SetValue(element, indexer.Identifier, ArrayIndexers.NoIndexers);
            }
            current[arrayProperty.Value] = collection.Append((element as ExpandoObject)!).ToList();
        }

        return (element as ExpandoObject)!;
    }

    static IEnumerable<ExpandoObject> EnsureCollection(IDictionary<string, object> current, PropertyPath property, ArrayProperty arrayProperty)
    {
        if (!current.TryGetValue(arrayProperty.Value, out var existing))
        {
            IEnumerable<ExpandoObject> empty = [];
            current[arrayProperty.Value] = empty;
            return empty;
        }

        if (existing is not IEnumerable enumerable)
        {
            throw new SegmentValueIsNotCollection(property, arrayProperty);
        }

        return enumerable.OfType<ExpandoObject>().ToList();
    }

    static IDictionary<string, object>? FindElement(IEnumerable<ExpandoObject> collection, ArrayIndexer indexer)
    {
        if (!indexer.IdentifierProperty.IsSet &&
            indexer.Identifier is int index &&
            collection.Count() > index)
        {
            return collection.ToArray()[index]!;
        }

        return collection
            .Cast<IDictionary<string, object>>()
            .SingleOrDefault(item =>
                ChildIdentity.TryGetValue(item, indexer.IdentifierProperty, out var identifierValue) &&
                identifierValue!.IsEqualTo(indexer.Identifier));
    }
}
