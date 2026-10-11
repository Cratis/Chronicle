// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Dynamic;

namespace Cratis.Chronicle.Projections;

/// <summary>
/// Applies the changes a projection produces to read models held in memory, the way a sink applies them to the read
/// models it stores.
/// </summary>
internal static class InMemoryChanges
{
    /// <summary>
    /// Apply changes to the state of a read model.
    /// </summary>
    /// <param name="key">The <see cref="Key"/> the changes were made for.</param>
    /// <param name="changes">The changes to apply.</param>
    /// <param name="state">The state to apply the changes to.</param>
    /// <returns>The state with the changes applied.</returns>
    public static ExpandoObject Apply(Key key, IEnumerable<Change> changes, ExpandoObject state)
    {
        var changesToApply = changes.ToList();
        var collectionPathsWithChildOperations = changesToApply.GetCollectionPathsWithChildOperations();
        var wholeCollectionReplacementPaths = changesToApply.GetWholeCollectionReplacementPaths();

        foreach (var change in changesToApply)
        {
            switch (change)
            {
                case PropertiesChanged<ExpandoObject> propertiesChanged:
                    state = propertiesChanged.ApplyToStateWithoutChildOperationConflicts(state, collectionPathsWithChildOperations, wholeCollectionReplacementPaths);
                    break;

                case ChildAdded childAdded:
                    var items = state.EnsureCollection<object>(childAdded.ChildrenProperty, childAdded.ArrayIndexers);
                    items.Add(childAdded.Child);
                    break;

                case ChildRemoved childRemoved:
                    var children = state.EnsureCollection<object>(childRemoved.ChildrenProperty, key.ArrayIndexers);
                    var childToRemove = children.FindByKey(childRemoved.IdentifiedByProperty, childRemoved.Key);
                    if (childToRemove is not null)
                    {
                        children.Remove(childToRemove);
                    }

                    break;

                case Joined joined:
                    state = Apply(key, joined.Changes, state);
                    break;

                case ResolvedJoin resolvedJoin:
                    state = Apply(key, resolvedJoin.Changes, state);
                    break;
            }
        }

        return state;
    }
}
