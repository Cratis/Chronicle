// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Properties;
using Cratis.Collections;

namespace Cratis.Chronicle.Changes;

public partial class Changeset<TSource, TTarget>
{
    static void ConsolidateJoinsAgainstChildrenAdded(Changeset<TSource, TTarget>? parent)
    {
        if (parent is null)
        {
            return;
        }

        // Note: This merges any properties changed on any ResolvedJoin that matches a ChildAdded
        // by its key, array property and identifier property. It takes the properties and resolves
        // them onto the child added. This avoids anyone working with a changeset to have to perform an
        // add and then perform an update on the child.
        // A merged join is removed from the parent: the child already carries its properties, and a join
        // left in place makes sinks apply them a second time. The changeset behind the join stays linked
        // to the child it merged into, and forwards to that child every PropertiesChanged<TTarget> added
        // to it from then on. That is the only change kind forwarded - a ChildAdded, ChildRemoved,
        // Removed or NestedCleared added to a merged join's changeset is consumed with the join and
        // never reaches the parent. Each difference is applied exactly once, by the merge or by the
        // link, so a value written to the child afterwards stands.
        var changes = parent._changes;
        var resolvesToRemove = new List<ResolvedJoin>();
        foreach (var childAdded in changes.OfType<ChildAdded>())
        {
            var resolvedJoins = changes.OfType<ResolvedJoin>().Where(_ => IsJoinForChild(_, childAdded)).ToArray();
            foreach (var resolvedJoin in resolvedJoins)
            {
                MergeJoinIntoChild(parent, resolvedJoin, childAdded);
            }

            resolvesToRemove.AddRange(resolvedJoins);
        }

        resolvesToRemove.ForEach(_ => changes.Remove(_));
        if (resolvesToRemove.Count > 0)
        {
            parent.SortChildrenAddedInThisChangeset(parent.CurrentState);
        }
    }

    static bool IsJoinForChild(ResolvedJoin resolvedJoin, ChildAdded childAdded) =>
        resolvedJoin.ArrayIndexers.All.Any(a =>
            a.ArrayProperty == childAdded.ChildrenProperty &&
            a.IdentifierProperty == childAdded.IdentifiedByProperty &&
            a.Identifier == childAdded.Key);

    static void MergeJoinIntoChild(Changeset<TSource, TTarget> parent, ResolvedJoin resolvedJoin, ChildAdded childAdded)
    {
        ApplyDifferencesToChild(
            childAdded.Child,
            resolvedJoin.Changes.OfType<PropertiesChanged<TTarget>>().SelectMany(_ => _.Differences));

        parent.LinkConsolidatedJoinToChild(resolvedJoin, childAdded);
    }

    static void ApplyDifferencesToChild(object child, IEnumerable<PropertyDifference> differences) =>
        differences.ForEach(difference => difference.ApplyToChild(child));

    static ChildAdded? FindMatchingChildAdded(List<ChildAdded> childAddedChanges, PropertyDifference diff)
    {
        // A PropertiesChanged targets a child if:
        // 1. The property path points to a child within an array
        // 2. The array indexers match a ChildAdded's key and array property
        if (diff.ArrayIndexers.IsEmpty)
        {
            return null;
        }

        return childAddedChanges.FirstOrDefault(childAdded => diff.ArrayIndexers.All.Any(indexer => IsIndexerForChild(indexer, childAdded)));
    }

    static bool IsIndexerForChild(ArrayIndexer indexer, ChildAdded childAdded) =>
        indexer.ArrayProperty == childAdded.ChildrenProperty &&
        indexer.IdentifierProperty == childAdded.IdentifiedByProperty &&
        Equals(indexer.Identifier, childAdded.Key);

    static List<PropertyDifference> ApplyDifferencesToMatchingChildren(List<ChildAdded> childAddedChanges, PropertiesChanged<TTarget> propertiesChanged)
    {
        // Keep the differences that don't match any ChildAdded, apply the rest to the child they match
        var remainingDifferences = new List<PropertyDifference>();
        foreach (var diff in propertiesChanged.Differences)
        {
            var matchingChildAdded = FindMatchingChildAdded(childAddedChanges, diff);
            if (matchingChildAdded is null)
            {
                remainingDifferences.Add(diff);
                continue;
            }

            diff.ApplyToChild(matchingChildAdded.Child);
        }

        return remainingDifferences;
    }

    static bool TryMergeIntoAncestor(List<PropertyDifferenceInChange> retainedDifferences, PropertyDifference difference, object state)
    {
        var ancestorIndex = retainedDifferences.FindIndex(_ =>
            PropertyDifferenceOverlap.HasSameArrayIndexers(_.Difference, difference) &&
            PropertyDifferenceOverlap.IsSameAsOrDescendantOf(difference.PropertyPath, _.Difference.PropertyPath));

        if (ancestorIndex < 0)
        {
            return false;
        }

        var ancestor = retainedDifferences[ancestorIndex];
        retainedDifferences[ancestorIndex] = ancestor with
        {
            Difference = PropertyDifferenceOverlap.CreateUpdatedDifferenceFor(ancestor.Difference, difference, state)
        };
        return true;
    }

    static List<PropertyDifferenceInChange> RetainOutermostDifferences((int Index, PropertiesChanged<TTarget> Change)[] propertyChanges)
    {
        var retainedDifferences = new List<PropertyDifferenceInChange>();

        foreach (var (index, propertiesChanged) in propertyChanges)
        {
            foreach (var difference in propertiesChanged.Differences)
            {
                if (TryMergeIntoAncestor(retainedDifferences, difference, propertiesChanged.State))
                {
                    continue;
                }

                retainedDifferences.RemoveAll(_ =>
                    PropertyDifferenceOverlap.HasSameArrayIndexers(_.Difference, difference) &&
                    PropertyDifferenceOverlap.IsSameAsOrDescendantOf(_.Difference.PropertyPath, difference.PropertyPath));

                retainedDifferences.Add(new(index, difference));
            }
        }

        return retainedDifferences;
    }

    static bool TryRemoveConflictingDifferences(
        PropertiesChanged<TTarget> propertiesChanged,
        ISet<PropertyPath> arraysWithChildOperations,
        out PropertiesChanged<TTarget>? replacement)
    {
        // Filter out property differences that conflict with child operations
        var nonConflictingDifferences = propertiesChanged.Differences
            .Where(diff => !diff.ConflictsWithChildOperation(arraysWithChildOperations))
            .ToList();

        // Only keep the PropertiesChanged if there are non-conflicting differences
        replacement = nonConflictingDifferences.Count != 0
            ? new PropertiesChanged<TTarget>(propertiesChanged.State, nonConflictingDifferences)
            : null;
        return nonConflictingDifferences.Count != propertiesChanged.Differences.Count();
    }

    void LinkConsolidatedJoinToChild(ResolvedJoin resolvedJoin, ChildAdded childAdded) =>
        _resolvedJoinChangesets
            .Find(_ => ReferenceEquals(_.Join, resolvedJoin))?
            .Changeset._consolidatedIntoChildren.Add(childAdded);

    void ApplyToConsolidatedChildren(Change change)
    {
        if (_consolidatedIntoChildren.Count == 0 || change is not PropertiesChanged<TTarget> propertiesChanged)
        {
            return;
        }

        _consolidatedIntoChildren.ForEach(childAdded => ApplyDifferencesToChild(childAdded.Child, propertiesChanged.Differences));
        parent?.SortChildrenAddedInThisChangeset(parent.CurrentState);
    }

    void Consolidate()
    {
        ConsolidateJoinsAgainstChildrenAdded(parent);

        // ConsolidatePropertiesChangedIntoChildAdded and ConsolidateConflictingOperations only ever act
        // on changesets that carry child collection operations; without any they return without touching
        // the changes, so skipping them keeps the outcome identical while avoiding the per-add rescans.
        if (_hasChildCollectionOperations)
        {
            ConsolidatePropertiesChangedIntoChildAdded();
        }

        ConsolidateOverlappingPropertiesChanged();

        if (_hasChildCollectionOperations)
        {
            ConsolidateConflictingOperations();
        }
    }

    void ConsolidatePropertiesChangedIntoChildAdded()
    {
        // Find all ChildAdded changes and their matching PropertiesChanged
        var childAddedChanges = _changes.OfType<ChildAdded>().ToList();
        if (childAddedChanges.Count == 0)
        {
            return;
        }

        var changesToRemove = new List<int>();

        for (var i = 0; i < _changes.Count; i++)
        {
            if (_changes[i] is not PropertiesChanged<TTarget> propertiesChanged)
            {
                continue;
            }

            var remainingDifferences = ApplyDifferencesToMatchingChildren(childAddedChanges, propertiesChanged);

            // If all differences were consolidated, mark this PropertiesChanged for removal
            if (remainingDifferences.Count == 0)
            {
                changesToRemove.Add(i);
            }

            // If some differences remain, update the PropertiesChanged
            else if (remainingDifferences.Count != propertiesChanged.Differences.Count())
            {
                _changes[i] = new PropertiesChanged<TTarget>(propertiesChanged.State, remainingDifferences);
            }
        }

        // Remove PropertiesChanged that were fully consolidated, in reverse order
        foreach (var index in changesToRemove.OrderDescending())
        {
            _changes.RemoveAt(index);
        }
    }

    void ConsolidateOverlappingPropertiesChanged()
    {
        var propertyChanges = _changes
            .Select((change, index) => (Index: index, Change: change as PropertiesChanged<TTarget>))
            .Where(_ => _.Change is not null)
            .Select(_ => (_.Index, Change: _.Change!))
            .ToArray();

        if (propertyChanges.Length == 0)
        {
            return;
        }

        var retainedByChange = RetainOutermostDifferences(propertyChanges)
            .GroupBy(_ => _.ChangeIndex)
            .ToDictionary(_ => _.Key, _ => _.Select(difference => difference.Difference).ToArray());

        foreach (var (index, propertiesChanged) in propertyChanges.OrderByDescending(_ => _.Index))
        {
            retainedByChange.TryGetValue(index, out var differences);
            ReplaceWithRetainedDifferences(index, propertiesChanged, differences);
        }
    }

    void ReplaceWithRetainedDifferences(int index, PropertiesChanged<TTarget> propertiesChanged, PropertyDifference[]? differences)
    {
        if (differences is null || differences.Length == 0)
        {
            _changes.RemoveAt(index);
            return;
        }

        if (!differences.SequenceEqual(propertiesChanged.Differences))
        {
            _changes[index] = new PropertiesChanged<TTarget>(propertiesChanged.State, differences);
        }
    }

    void ConsolidateConflictingOperations()
    {
        var arraysWithChildOperations = _changes.GetCollectionPathsWithChildOperations();
        if (arraysWithChildOperations.Count == 0)
        {
            return;
        }

        var changesToUpdate = new List<(int Index, Change? Change)>();

        for (var i = 0; i < _changes.Count; i++)
        {
            if (_changes[i] is PropertiesChanged<TTarget> propertiesChanged &&
                TryRemoveConflictingDifferences(propertiesChanged, arraysWithChildOperations, out var replacement))
            {
                changesToUpdate.Add((i, replacement));
            }
        }

        // Apply updates in reverse order to avoid index shifting
        foreach (var (index, change) in changesToUpdate.OrderByDescending(t => t.Index))
        {
            if (change is null)
            {
                _changes.RemoveAt(index);
            }
            else
            {
                _changes[index] = change;
            }
        }
    }
}
