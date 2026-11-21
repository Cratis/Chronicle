// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

extern alias KernelConcepts;

using System.Collections;
using System.Dynamic;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Dynamic;
using Cratis.Chronicle.Properties;
using Cratis.Serialization;
using KernelKey = KernelConcepts::Cratis.Chronicle.Concepts.Keys.Key;

namespace Cratis.Chronicle.Testing.ReadModels;

internal static partial class ProjectionReadModelProcessor
{
    /// <summary>
    /// Mirrors MongoDB's `_id` → identifier property mapping for the in-memory test harness.
    /// Finds the read model's identifier property (preferring <c language="csharp">[Key]</c>, then <c language="csharp">[Subject]</c>,
    /// then a property named "Id" by convention) and writes the resolved projection key value into
    /// the state under that property's camel-cased name — unless an event mapping has already
    /// populated that property.
    /// </summary>
    /// <param name="state">The projection state ExpandoObject to inject into.</param>
    /// <param name="key">The resolved projection key, or <see langword="null"/> if no event resolved one.</param>
    /// <param name="readModelType">The runtime read model type.</param>
    internal static void InjectIdentifierFromKey(ExpandoObject state, KernelKey? key, Type readModelType)
    {
        if (key is null || key.Value is null) return;

        var identifierProperty = IdentifierProperty.Find(readModelType);
        if (identifierProperty is null) return;

        var propertyName = AcronymFriendlyJsonCamelCaseNamingPolicy.Instance.ConvertName(identifierProperty.Name);
        var stateDict = (IDictionary<string, object?>)state;

        // Don't overwrite an explicit projection mapping (e.g. [SetFrom<T>] or [SetFromContext<T>]
        // that wrote to the identifier property). The MongoDB analogue is: if the projection
        // updates `_id`, that update wins; we only fill in `_id` when no update touched it.
        if (stateDict.TryGetValue(propertyName, out var existing) && existing is not null)
        {
            return;
        }

        stateDict[propertyName] = key.Value;
    }

    /// <summary>
    /// Applies the changes of a changeset to the state of an instance.
    /// </summary>
    /// <param name="key">The resolved <see cref="KernelKey"/> of the instance.</param>
    /// <param name="changes">The <see cref="Change"/> items to apply.</param>
    /// <param name="state">The state to apply the changes to.</param>
    /// <returns>The state with the changes applied.</returns>
    internal static ExpandoObject ApplyActualChanges(KernelKey key, IEnumerable<Change> changes, ExpandoObject state)
    {
        foreach (var change in changes)
        {
            state = ApplyActualChange(key, change, state);
        }

        return state;
    }

    /// <summary>
    /// Removes the item whose identifier equals <paramref name="key"/> from every matching leaf collection
    /// along the path described by <paramref name="segments"/>.
    /// </summary>
    /// <param name="current">The object that holds the collection the first remaining segment names.</param>
    /// <param name="segments">The remaining children property path segments.</param>
    /// <param name="identifiedByProperty">The property that identifies a child within its collection.</param>
    /// <param name="key">The identifier of the child to remove.</param>
    internal static void RemoveFromCollection(IDictionary<string, object?> current, string[] segments, PropertyPath identifiedByProperty, object key)
    {
        if (segments.Length == 0 || !TryReadCollection(current, segments[0], out var items))
        {
            return;
        }

        if (segments.Length == 1)
        {
            items.RemoveAll(item => IsChildWithIdentity(item, identifiedByProperty, key));
            current[segments[0]] = items;
            return;
        }

        // Intermediate collection — recurse into each element to reach the leaf collection.
        foreach (var item in items.OfType<ExpandoObject>())
        {
            RemoveFromCollection(item, segments[1..], identifiedByProperty, key);
        }
    }

    /// <summary>
    /// Finds, or creates, the collection a <see cref="ChildAdded"/> change adds its child to, creating any
    /// missing parents along the way.
    /// </summary>
    /// <param name="state">The state to find the collection in.</param>
    /// <param name="childAdded">The <see cref="ChildAdded"/> change.</param>
    /// <returns>The collection held by the state that the child is added to.</returns>
    internal static List<object> EnsureChildCollection(ExpandoObject state, ChildAdded childAdded)
    {
        var normalizedIndexers = NormalizeArrayIndexers(childAdded.ArrayIndexers).All.ToArray();
        var current = (IDictionary<string, object?>)state;

        foreach (var indexer in normalizedIndexers.Take(Math.Max(0, normalizedIndexers.Length - 1)))
        {
            current = FindOrCreateParent(current, indexer);
        }

        var targetCollectionName = childAdded.ChildrenProperty.LastSegment.Value;
        var items = ReadCollection(current, targetCollectionName);
        current[targetCollectionName] = items;
        return items;
    }

    /// <summary>
    /// Unwraps a value held by the state when it is a specified/value wrapper.
    /// </summary>
    /// <param name="value">The value to normalize.</param>
    /// <returns>The wrapped value for a wrapper, otherwise the value itself.</returns>
    internal static object? NormalizeStateValue(object? value)
    {
        if (value is null)
        {
            return null;
        }

        var type = value.GetType();
        var isSpecified = type.GetProperty("IsSpecified");
        var wrappedValue = type.GetProperty("Value");

        if (isSpecified?.PropertyType == typeof(bool) && wrappedValue is not null)
        {
            return wrappedValue.GetValue(value);
        }

        return value;
    }

    /// <summary>
    /// Writes the key of an added child onto the child, under the property that identifies it.
    /// </summary>
    /// <param name="childAdded">The <see cref="ChildAdded"/> change.</param>
    internal static void InjectChildIdentifierFromKey(ChildAdded childAdded)
    {
        if (childAdded.Child is not ExpandoObject childState || !IsIdentifyingProperty(childAdded.IdentifiedByProperty))
        {
            return;
        }

        childAdded.IdentifiedByProperty.SetValue(childState, NormalizeStateValue(childAdded.Key)!, ArrayIndexers.NoIndexers);
    }

    static ExpandoObject ApplyActualChange(KernelKey key, Change change, ExpandoObject state)
    {
        switch (change)
        {
            case PropertiesChanged<ExpandoObject> propertiesChanged:
                ApplyPropertyDifferences(propertiesChanged.Differences, state);
                break;

            case ChildAdded childAdded:
                InjectChildIdentifierFromKey(childAdded);
                var items = EnsureChildCollection(state, childAdded);
                items.InsertChild(childAdded.Child, childAdded.Ordering, childAdded.IdentifiedByProperty);
                break;

            case ChildRemoved childRemoved:
                ApplyChildRemoved(state, childRemoved);
                break;

            case NestedCleared nestedCleared:
                ClearNested(state, nestedCleared);
                break;

            case Joined joined:
                state = ApplyActualChanges(key, joined.Changes, state);
                break;

            case ResolvedJoin resolvedJoin:
                state = ApplyActualChanges(key, resolvedJoin.Changes, state);
                break;
        }

        return state;
    }

    static void ClearNested(ExpandoObject state, NestedCleared nestedCleared)
    {
        if (state.TryGetExistingPath(nestedCleared.NestedProperty, nestedCleared.ArrayIndexers) is { } existingParent)
        {
            ((IDictionary<string, object?>)existingParent)[nestedCleared.NestedProperty.LastSegment.Value] = null;
        }
    }

    /// <summary>
    /// Removes a child from its collection in the projection state, mirroring how the production sinks
    /// apply a <see cref="ChildRemoved"/> change. Walks the children property path and removes from every
    /// matching leaf collection the item whose identifier property equals the removed key.
    /// </summary>
    /// <param name="state">The projection state to mutate.</param>
    /// <param name="childRemoved">The <see cref="ChildRemoved"/> change to apply.</param>
    static void ApplyChildRemoved(ExpandoObject state, ChildRemoved childRemoved)
    {
        var segments = childRemoved.ChildrenProperty.Segments.Select(s => s.Value).ToArray();
        RemoveFromCollection(state, segments, childRemoved.IdentifiedByProperty, childRemoved.Key);
    }

    static void ApplyPropertyDifferences(IEnumerable<PropertyDifference> differences, ExpandoObject state)
    {
        foreach (var difference in differences)
        {
            if (difference.IsRemoval)
            {
                difference.PropertyPath.RemoveValue(state, NormalizeArrayIndexers(difference.ArrayIndexers));
                continue;
            }

            difference.PropertyPath.SetValue(state, NormalizeStateValue(difference.Changed)!, NormalizeArrayIndexers(difference.ArrayIndexers));
        }
    }

    static ExpandoObject FindOrCreateParent(IDictionary<string, object?> current, ArrayIndexer indexer)
    {
        var collectionName = indexer.ArrayProperty.LastSegment.Value;
        var collection = ReadCollection(current, collectionName);

        var element = FindParent(collection, indexer);
        if (element is not null)
        {
            return element;
        }

        element = new ExpandoObject();
        if (indexer.IdentifierProperty.IsSet)
        {
            indexer.IdentifierProperty.SetValue(element, NormalizeStateValue(indexer.Identifier)!, ArrayIndexers.NoIndexers);
        }

        collection.Add(element);
        current[collectionName] = collection;
        return element;
    }

    static ExpandoObject? FindParent(List<object> collection, ArrayIndexer indexer)
    {
        if (IsPositional(indexer, collection.Count))
        {
            return collection[(int)indexer.Identifier] as ExpandoObject;
        }

        return collection
            .OfType<ExpandoObject>()
            .SingleOrDefault(item => IsChildWithIdentity(item, indexer.IdentifierProperty, indexer.Identifier));
    }

    static bool IsPositional(ArrayIndexer indexer, int collectionCount) =>
        !indexer.IdentifierProperty.IsSet &&
        indexer.Identifier is int index &&
        collectionCount > index;

    static bool IsChildWithIdentity(object? item, PropertyPath identifiedByProperty, object? key) =>
        item is ExpandoObject expando &&
        ChildIdentity.TryGetValue(expando, identifiedByProperty, out var idValue) &&
        Equals(NormalizeStateValue(idValue), NormalizeStateValue(key));

    static bool TryReadCollection(IDictionary<string, object?> owner, string name, out List<object> items)
    {
        if (owner.TryGetValue(name, out var value) && value is IEnumerable enumerable)
        {
            items = enumerable.Cast<object>().ToList();
            return true;
        }

        items = [];
        return false;
    }

    static List<object> ReadCollection(IDictionary<string, object?> owner, string name)
    {
        TryReadCollection(owner, name, out var items);
        return items;
    }

    static bool IsIdentifyingProperty(PropertyPath property) => property.IsSet && !property.IsRoot;

    static ArrayIndexers NormalizeArrayIndexers(ArrayIndexers arrayIndexers) =>
        arrayIndexers.IsEmpty
            ? ArrayIndexers.NoIndexers
            : new ArrayIndexers(arrayIndexers.All.Select(indexer => indexer with { Identifier = NormalizeStateValue(indexer.Identifier)! }));
}
