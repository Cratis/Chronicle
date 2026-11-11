// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;
using System.Dynamic;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Dynamic;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Storage;

namespace Cratis.Chronicle.Projections.Engine.Pipelines.Steps;

/// <summary>
/// Locates the parent of a future in the current read model state, collecting the chain of <see cref="ArrayIndexer"/>
/// entries needed to navigate from the root to that parent.
/// </summary>
internal static class FutureParentSearch
{
    /// <summary>
    /// Checks whether the parent item exists in the current state and, if so, collects the full
    /// chain of <see cref="ArrayIndexer"/> entries needed to navigate from root to that parent.
    /// </summary>
    /// <param name="search">The <see cref="ParentSearch"/> describing the parent being located.</param>
    /// <param name="allIndexers">Receives the full root-to-parent indexer chain when the method returns <see langword="true"/>.</param>
    /// <remarks>
    /// For a first-level child the root read model is the parent, so no ancestor indexers are
    /// needed. For a child of an array element, <paramref name="allIndexers"/> receives a single
    /// entry for the parent's collection. For deeper hierarchies the method
    /// recurses through each intermediate array, locating the enclosing element at each level and
    /// recording its identifier, so that EnsurePath can navigate the full path
    /// without missing-indexer errors.
    /// </remarks>
    /// <returns><see langword="true"/> if the parent was found; otherwise <see langword="false"/>.</returns>
    internal static bool TryFindParentWithIndexers(in ParentSearch search, out List<ArrayIndexer> allIndexers)
    {
        allIndexers = [];
        if (search.CurrentState is not { } state) return false;

        var chain = BuildParentChain(search.ChildProjection);

        if (chain.Count == 0) return IsParentUnderRoot(search, state);
        if (chain.Count == 1) return TryFindInSingleLevel(search, state, chain[0], allIndexers);

        return new LevelWalk(search, state, chain, allIndexers).Search(0, []);
    }

    static bool IsParentUnderRoot(in ParentSearch search, ExpandoObject state)
    {
        // A first-level child can be created under the root or updated after it already
        // exists there. The creator's parent key is the root key, whereas an update whose
        // parent key comes from its event source names the child in the root collection.
        // Neither may resolve against a child-created placeholder before the root's FROM.
        if (!IsRootInitialized(state, search.Changeset)) return false;
        if (KeyValueEquality.AreEqual(search.RootKey, search.ParentKey)) return true;

        var directCollection = AsExpandoCollection(search.ChildProjection.ChildrenPropertyPath.GetValue(state, ArrayIndexers.NoIndexers));
        return directCollection?.ToList().Contains(search.ParentIdentifiedByProperty, search.ParentKey) == true;
    }

    static bool TryFindInSingleLevel(in ParentSearch search, ExpandoObject state, IProjection ancestor, List<ArrayIndexer> allIndexers)
    {
        var collection = AsExpandoCollection(ancestor.ChildrenPropertyPath.GetValue(state, ArrayIndexers.NoIndexers));
        if (collection is null) return false;

        var collectionList = collection.ToList();
        if (!collectionList.Contains(search.ParentIdentifiedByProperty, search.ParentKey)) return false;

        var actualKey = GetActualStoredKey(collectionList, search.ParentIdentifiedByProperty, search.ParentKey);
        allIndexers.Add(new ArrayIndexer(search.ParentPath, search.ParentIdentifiedByProperty, actualKey));
        return true;
    }

    static bool IsRootInitialized(ExpandoObject state, IChangeset<AppendedEvent, ExpandoObject> changeset)
    {
        if (changeset.HasBeenRemoved()) return false;

        var initializedNow = changeset.Changes
            .OfType<PropertiesChanged<ExpandoObject>>()
            .SelectMany(change => change.Differences)
            .LastOrDefault(difference => difference.PropertyPath.Path == WellKnownProperties.ReadModelInstanceInitialized);
        if (initializedNow is not null)
        {
            return initializedNow.Changed is true;
        }

        return ((IDictionary<string, object?>)state).TryGetValue(WellKnownProperties.ReadModelInstanceInitialized, out var initialized) && initialized is true;
    }

    /// <summary>
    /// Builds the projection chain from the root-level child projection down to the direct parent of
    /// <paramref name="childProjection"/>, in root-to-leaf order (index 0 = closest to root).
    /// </summary>
    /// <param name="childProjection">The projection whose ancestor chain is being built.</param>
    /// <returns>Ordered list of ancestor projections, from root-most to the direct parent.</returns>
    static List<IProjection> BuildParentChain(IProjection childProjection)
    {
        var chain = new List<IProjection>();
        var current = childProjection.Parent;
        while (current is not null)
        {
            var path = current.ChildrenPropertyPath;
            if (!path.IsSet || path.IsRoot) break;

            chain.Insert(0, current);
            current = current.Parent;
        }

        return chain;
    }

    /// <summary>
    /// Returns the value actually stored in the collection for the given identity property and key,
    /// using type-converting lookup so that type mismatches (e.g. <c language="csharp">EventSourceId</c> vs <c language="csharp">Guid</c>)
    /// are resolved. Falls back to <paramref name="key"/> when the item cannot be found.
    /// </summary>
    /// <param name="collection">The collection to search.</param>
    /// <param name="identifiedByProperty">The property that holds the identifier on each item.</param>
    /// <param name="key">The key to look up.</param>
    /// <returns>The stored identifier value, or <paramref name="key"/> if not found.</returns>
    static object GetActualStoredKey(IEnumerable<ExpandoObject> collection, PropertyPath identifiedByProperty, object key) =>
        ReadIdentifier(collection.GetItem(identifiedByProperty, key), identifiedByProperty) ?? key;

    static object? ReadIdentifier(object? item, PropertyPath identifiedByProperty) =>
        item is IDictionary<string, object> dictionary && dictionary.TryGetValue(identifiedByProperty.Path, out var value) ? value : null;

    static IEnumerable<ExpandoObject>? AsExpandoCollection(object? value) =>
        value switch
        {
            IEnumerable<ExpandoObject> expando => expando,
            IEnumerable enumerable => enumerable.OfType<ExpandoObject>(),
            _ => null
        };

    /// <summary>
    /// Recursively navigates the current state through the levels of the projection chain, searching
    /// every element of the current array for a descendant that ultimately contains the target parent
    /// key. When found, the full indexer chain (one entry per level, root-to-leaf) is written to the result.
    /// </summary>
    sealed class LevelWalk(ParentSearch search, ExpandoObject state, List<IProjection> chain, List<ArrayIndexer> result)
    {
        /// <summary>
        /// Search one level of the chain.
        /// </summary>
        /// <param name="level">The current depth within the chain.</param>
        /// <param name="currentIndexers">Array indexers accumulated so far for levels above this one.</param>
        /// <returns><see langword="true"/> if the parent was found at or below this level; otherwise <see langword="false"/>.</returns>
        internal bool Search(int level, List<ArrayIndexer> currentIndexers)
        {
            var childrenPath = chain[level].ChildrenPropertyPath;

            // Navigate from the root using the full children path with accumulated indexers so
            // that multi-segment paths (e.g. "[Configurations].[Hubs]") are resolved correctly.
            var collection = AsExpandoCollection(childrenPath.GetValue(state, new ArrayIndexers(currentIndexers)));
            if (collection is null) return false;

            return level == chain.Count - 1
                ? MatchLeaf(collection, childrenPath, currentIndexers)
                : DescendInto(collection, level, currentIndexers);
        }

        bool MatchLeaf(IEnumerable<ExpandoObject> collection, PropertyPath childrenPath, List<ArrayIndexer> currentIndexers)
        {
            var leafList = collection.ToList();
            if (!leafList.Contains(search.ParentIdentifiedByProperty, search.ParentKey)) return false;

            result.AddRange(currentIndexers);
            var actualKey = GetActualStoredKey(leafList, search.ParentIdentifiedByProperty, search.ParentKey);
            result.Add(new ArrayIndexer(childrenPath, search.ParentIdentifiedByProperty, actualKey));
            return true;
        }

        bool DescendInto(IEnumerable<ExpandoObject> collection, int level, List<ArrayIndexer> currentIndexers)
        {
            var childrenPath = chain[level].ChildrenPropertyPath;
            var identifiedBy = chain[level].IdentifiedByProperty;
            foreach (var item in collection)
            {
                if (ReadIdentifier(item, identifiedBy) is not { } itemId) continue;

                var nextIndexers = new List<ArrayIndexer>(currentIndexers)
                {
                    new(childrenPath, identifiedBy, itemId)
                };

                if (Search(level + 1, nextIndexers)) return true;
            }

            return false;
        }
    }
}
