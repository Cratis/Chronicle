// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;
using System.Dynamic;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Dynamic;

namespace Cratis.Chronicle.Storage.InMemory.Sinks;

public partial class InMemorySink
{
    static PropertiesChanged<ExpandoObject> Detached(PropertiesChanged<ExpandoObject> propertiesChanged)
    {
        if (!propertiesChanged.Differences.Any(difference => NeedsDetaching(difference.Changed)))
        {
            return propertiesChanged;
        }

        return propertiesChanged with
        {
            Differences = [.. propertiesChanged.Differences.Select(difference =>
                new PropertyDifference(difference.PropertyPath, difference.Original, Detached(difference.Changed), difference.ArrayIndexers))]
        };
    }

    /// <summary>
    /// Copies a value taken from a change, so the row it is inserted into owns it.
    /// </summary>
    /// <param name="value">The value to copy.</param>
    /// <returns>The value itself when it cannot be shared, otherwise a deep copy.</returns>
    /// <remarks>
    /// A value taken from a change is shared with the changeset, and a join hands the same change to every row it
    /// matches. Inserting it as-is would let one row, the changeset or a later candidate change another row's state.
    /// </remarks>
    static object? Detached(object? value)
    {
        if (!NeedsDetaching(value))
        {
            return value;
        }

        if (value is ExpandoObject expando)
        {
            return expando.Clone();
        }

        var holder = new ExpandoObject();
        ((IDictionary<string, object?>)holder)[nameof(value)] = value;
        return ((IDictionary<string, object?>)holder.Clone())[nameof(value)];
    }

    static bool NeedsDetaching(object? value) => value is ExpandoObject or (IEnumerable and not string and not byte[]);

    static bool HoldsChild(ExpandoObject document, ChildRemovedFromAll childRemoved) =>
        VisitChild(document, ChildrenPathOf(childRemoved), 0, childRemoved, remove: false);

    static void RemoveChildFromDocument(ExpandoObject document, ChildRemovedFromAll childRemoved) =>
        VisitChild(document, ChildrenPathOf(childRemoved), 0, childRemoved, remove: true);

    static string[] ChildrenPathOf(ChildRemovedFromAll childRemoved) =>
        [.. childRemoved.ChildrenProperty.Segments.Select(_ => _.Value)];

    /// <summary>
    /// Finds a child, and removes it when asked to, so deciding whether a published row is affected never changes it.
    /// </summary>
    /// <param name="document">The document to look in.</param>
    /// <param name="segments">The segments of the path to the children.</param>
    /// <param name="index">The segment being visited.</param>
    /// <param name="childRemoved">The <see cref="ChildRemovedFromAll"/> identifying the child.</param>
    /// <param name="remove">Whether to remove the child when it is found.</param>
    /// <returns>True when the document holds the child.</returns>
    static bool VisitChild(IDictionary<string, object?> document, string[] segments, int index, ChildRemovedFromAll childRemoved, bool remove)
    {
        if (index >= segments.Length || !document.TryGetValue(segments[index], out var value) || value is null)
        {
            return false;
        }

        if (index == segments.Length - 1)
        {
            return VisitChildCollection(value, childRemoved, remove);
        }

        if (value is IDictionary<string, object?> nested)
        {
            return VisitChild(nested, segments, index + 1, childRemoved, remove);
        }

        var found = false;
        if (value is IEnumerable collection)
        {
            foreach (var item in collection.OfType<IDictionary<string, object?>>())
            {
                found |= VisitChild(item, segments, index + 1, childRemoved, remove);
            }
        }

        return found;
    }

    static bool VisitChildCollection(object value, ChildRemovedFromAll childRemoved, bool remove)
    {
        var child = value is IList children ? children.Cast<object>().FindByKey(childRemoved.IdentifiedByProperty, childRemoved.Key) : null;
        if (child is not null && remove)
        {
            ((IList)value).Remove(child);
        }

        return child is not null;
    }

    ExpandoObject ApplyActualChanges(Key key, IEnumerable<Change> changes, ExpandoObject state)
    {
        var changesToApply = changes.ToList();
        var collectionPathsWithChildOperations = changesToApply.GetCollectionPathsWithChildOperations();
        var wholeCollectionReplacementPaths = changesToApply.GetWholeCollectionReplacementPaths();

        foreach (var change in changesToApply)
        {
            switch (change)
            {
                case PropertiesChanged<ExpandoObject> propertiesChanged:
                    state = Detached(propertiesChanged).ApplyToStateWithoutChildOperationConflicts(state, collectionPathsWithChildOperations, wholeCollectionReplacementPaths);
                    break;

                case ChildAdded childAdded:
                    var collection = state.EnsureCollection<ExpandoObject, object>(childAdded.ChildrenProperty, childAdded.ArrayIndexers);
                    collection.Add(Detached(childAdded.State)!);
                    break;

                case ChildRemoved childRemoved:
                    var childCollection = state.EnsureCollection<ExpandoObject, object>(childRemoved.ChildrenProperty, key.ArrayIndexers);
                    var childToRemove = childCollection.FindByKey(childRemoved.IdentifiedByProperty, childRemoved.Key);
                    if (childToRemove is not null)
                    {
                        childCollection.Remove(childToRemove);
                    }

                    break;

                case NestedCleared nestedCleared:
                    if (state.TryGetExistingPath(nestedCleared.NestedProperty, nestedCleared.ArrayIndexers) is { } existingParent)
                    {
                        ((IDictionary<string, object?>)existingParent)[nestedCleared.NestedProperty.LastSegment.Value] = null;
                    }
                    break;

                case Joined joined:
                    state = ApplyActualChanges(key, joined.Changes, state);
                    break;

                case ResolvedJoin resolvedJoin:
                    state = ApplyActualChanges(key, resolvedJoin.Changes, state);
                    break;
            }
        }

        return state;
    }
}
