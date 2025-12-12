// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Objects;
using Cratis.Chronicle.Properties;
using Cratis.Collections;

namespace Cratis.Chronicle.Changes;

/// <summary>
/// Represents an implementation of <see cref="IChangeset{TSource, TTarget}"/>.
/// </summary>
/// <typeparam name="TSource">Type of the source object we are working from.</typeparam>
/// <typeparam name="TTarget">Type of target object we are applying changes to.</typeparam>
/// <remarks>
/// Initializes a new instance of <see cref="Changeset{TSource, TTarget}"/>.
/// </remarks>
/// <param name="comparer"><see cref="IObjectComparer"/> to compare objects with.</param>
/// <param name="incoming"><see cref="Incoming"/> that the <see cref="Changeset{TSource, TTarget}"/> is for.</param>
/// <param name="initialState">The initial state before any changes are applied.</param>
/// <param name="parent">Optional parent <see cref="IChangeset{TSource, TTarget}"/> if any.</param>
public class Changeset<TSource, TTarget>(IObjectComparer comparer, TSource incoming, TTarget initialState, Changeset<TSource, TTarget>? parent = null) : IChangeset<TSource, TTarget>
{
    readonly List<Change> _changes = [];
    TTarget _initialState = initialState;

    /// <inheritdoc/>
    public TSource Incoming { get; set; } = incoming;

    /// <inheritdoc/>
    public TTarget InitialState
    {
        get => _initialState;
        set
        {
            _initialState = value;
            CurrentState = value;
        }
    }

    /// <inheritdoc/>
    public TTarget CurrentState { get; private set; } = initialState;

    /// <inheritdoc/>
    public IEnumerable<Change> Changes => _changes;

    /// <inheritdoc/>
    public bool HasChanges => _changes.Count > 0;

    /// <inheritdoc/>
    public void Add(Change change)
    {
        _changes.Add(change);
        Consolidate();
    }

    /// <inheritdoc/>
    public void SetProperties(IEnumerable<PropertyMapper<TSource, TTarget>> propertyMappers, ArrayIndexers arrayIndexers)
    {
        var workingState = CurrentState.Clone()!;
        var differences = SetProperties(workingState, propertyMappers, arrayIndexers);

        if (differences.Count != 0)
        {
            Add(new PropertiesChanged<TTarget>(workingState, differences));
        }
        CurrentState = workingState;
    }

    /// <inheritdoc/>
    public IChangeset<TSource, TTarget> Join(PropertyPath onProperty, object key, ArrayIndexers arrayIndexers)
    {
        var workingState = CurrentState.Clone()!;
        var childChangeset = new Changeset<TSource, TTarget>(comparer, Incoming, workingState, this);
        Add(new Joined(workingState, key, onProperty, arrayIndexers, childChangeset.Changes));
        CurrentState = workingState;
        return childChangeset;
    }

    /// <inheritdoc/>
    public IChangeset<TSource, TTarget> ResolvedJoin(PropertyPath onProperty, object key, TSource incoming, ArrayIndexers arrayIndexers)
    {
        var workingState = CurrentState.Clone()!;
        var childChangeset = new Changeset<TSource, TTarget>(comparer, incoming, workingState, this);
        Add(new ResolvedJoin(workingState, key, onProperty, arrayIndexers, childChangeset.Changes));
        Consolidate();
        CurrentState = workingState;
        return childChangeset;
    }

    /// <inheritdoc/>
    public void AddChild(PropertyPath childrenProperty, object child)
    {
        var workingState = CurrentState.Clone()!;
        var items = workingState.EnsureCollection<TTarget, object>(childrenProperty, ArrayIndexers.NoIndexers);
        items.Add(child);
        Add(new ChildAdded(child, childrenProperty, PropertyPath.Root, null!, ArrayIndexers.NoIndexers));
        CurrentState = workingState;
    }

    /// <inheritdoc/>
    public void AddChild<TChild>(
        PropertyPath childrenProperty,
        PropertyPath identifiedByProperty,
        object key,
        IEnumerable<PropertyMapper<TSource, TTarget>> propertyMappers,
        ArrayIndexers arrayIndexers)
        where TChild : new()
    {
        var workingState = CurrentState.Clone();

        // Check if there's already a ChildAdded change that matches this child in the same parent context
        var parentKey = arrayIndexers.All.First().Identifier;
        var existingChildAdded = _changes
            .OfType<ChildAdded>()
            .FirstOrDefault(_ => Equals(_.Key.ToString(), parentKey.ToString()));

        if (existingChildAdded is not null)
        {
            // Optimize by updating the existing child in-memory instead of creating a new change
            ApplyPropertyMappers(workingState!, propertyMappers, arrayIndexers);

            var existingCollection = workingState.EnsureCollection<TTarget, object>(
                existingChildAdded.ChildrenProperty,
                existingChildAdded.ArrayIndexers);
            var childToUpdate = existingCollection.FindByKey(existingChildAdded.IdentifiedByProperty, existingChildAdded.Key);
            if (childToUpdate is null)
            {
                return;
            }

            // Replace the existing ChildAdded with a new one containing the updated child
            var index = _changes.IndexOf(existingChildAdded);
            _changes[index] = new ChildAdded(
                childToUpdate,
                existingChildAdded.ChildrenProperty,
                existingChildAdded.IdentifiedByProperty,
                existingChildAdded.Key,
                existingChildAdded.ArrayIndexers);

            // Update the item in the current state's collection
            var collection = CurrentState.EnsureCollection<TTarget, object>(childrenProperty, arrayIndexers);
            var existingItem = collection.FindByKey(identifiedByProperty, key);
            if (existingItem is not null && collection is IList<object> list)
            {
                var itemIndex = list.IndexOf(existingItem);
                list[itemIndex] = childToUpdate;
            }

            CurrentState = workingState;
            return;
        }

        var items = workingState.EnsureCollection<TTarget, object>(childrenProperty, arrayIndexers);
        object? item = null;

        if (identifiedByProperty.IsSet)
        {
            if (!items.Contains(identifiedByProperty, key))
            {
                // If the identified property is root, we want to add the item directly. That means
                // the object is identified by itself.
                if (identifiedByProperty.IsRoot)
                {
                    item = key;
                }
                else
                {
                    item = new TChild();
                    identifiedByProperty.SetValue(item, key, ArrayIndexers.NoIndexers);
                }
            }
        }
        else
        {
            item = new TChild();
            arrayIndexers = new ArrayIndexers(
            [
                arrayIndexers.All.First() with { Identifier = items.Count }
            ]);
        }

        if (item is not null)
        {
            items.Add(item);
            SetProperties(workingState, propertyMappers, arrayIndexers);
            Add(new ChildAdded(item, childrenProperty, identifiedByProperty, key!, arrayIndexers));
        }

        CurrentState = workingState;
    }

    /// <inheritdoc/>
    public void Remove()
    {
        Add(new Removed(CurrentState.Clone()!));
    }

    /// <inheritdoc/>
    public void RemoveChild(
        PropertyPath childrenProperty,
        PropertyPath identifiedByProperty,
        object key,
        ArrayIndexers arrayIndexers)
    {
        var workingState = CurrentState.Clone()!;
        var items = workingState.EnsureCollection<TTarget, object>(childrenProperty, arrayIndexers);

        var item = items.FindByKey(identifiedByProperty, key);
        if (item is not null)
        {
            items.Remove(item);
            Add(new ChildRemoved(item, childrenProperty, identifiedByProperty, key));
        }

        CurrentState = workingState;
    }

    /// <inheritdoc/>
    public void RemoveChildFromAll(
        PropertyPath childrenProperty,
        PropertyPath identifiedByProperty,
        object key,
        ArrayIndexers arrayIndexers)
    {
        Add(new ChildRemovedFromAll(childrenProperty, identifiedByProperty, key, arrayIndexers));
    }

    /// <inheritdoc/>
    public bool HasChildBeenAddedWithKey(PropertyPath childrenProperty, object key)
    {
        return Changes
                .Select(_ => _ as ChildAdded)
                .Any(_ => _ != null && _.ChildrenProperty == childrenProperty && _.Key == key);
    }

    /// <inheritdoc/>
    public bool HasBeenRemoved() => Changes.Any(_ => _ is Removed);

    /// <inheritdoc/>
    public bool HasJoined() =>
        Changes.OfType<Joined>().Any() ||
        Changes.OfType<ChildRemovedFromAll>().Any();

    /// <inheritdoc/>
    public bool HasRemoved() =>
        Changes.OfType<Removed>().Any() ||
        Changes.OfType<ChildRemoved>().Any();

    /// <inheritdoc/>
    public TChild GetChildByKey<TChild>(object key)
    {
        foreach (var change in _changes)
        {
            if (change is ChildAdded childAdded && childAdded.Key == key)
            {
                return (TChild)childAdded.Child;
            }
        }

        return default!;
    }

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
        var changes = parent._changes;
        var resolvesToRemove = new List<ResolvedJoin>();
        foreach (var childAdded in changes.OfType<ChildAdded>())
        {
            var resolvedJoins = changes
                .OfType<ResolvedJoin>().Where(_ =>
                    _.ArrayIndexers.All.Any(a =>
                        a.ArrayProperty == childAdded.ChildrenProperty &&
                        a.IdentifierProperty == childAdded.IdentifiedByProperty &&
                        a.Identifier == childAdded.Key));

            resolvedJoins
                .SelectMany(_ => _.Changes.OfType<PropertiesChanged<TTarget>>())
                .SelectMany(_ => _.Differences)
                .ForEach(difference =>
                {
                    var propertyPath = PropertyPath.CreateFrom([difference.PropertyPath.LastSegment]);
                    propertyPath.SetValue(childAdded.Child, difference.Changed!, difference.ArrayIndexers);
                });

            resolvesToRemove.AddRange(resolvedJoins);
        }

        resolvesToRemove.ForEach(_ => changes.Remove(_));
    }

    void Consolidate()
    {
        ConsolidateJoinsAgainstChildrenAdded(parent);
        ConsolidatePropertiesChangedIntoChildAdded();
        ConsolidateConflictingOperations();
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
            var change = _changes[i];
            if (change is PropertiesChanged<TTarget> propertiesChanged)
            {
                // Find matching ChildAdded for each property difference
                var remainingDifferences = new List<PropertyDifference>();

                foreach (var diff in propertiesChanged.Differences)
                {
                    var matchingChildAdded = FindMatchingChildAdded(childAddedChanges, diff);

                    if (matchingChildAdded != null)
                    {
                        // Apply the property change to the child
                        var propertyPath = PropertyPath.CreateFrom([diff.PropertyPath.LastSegment]);
                        propertyPath.SetValue(matchingChildAdded.Child, diff.Changed!, diff.ArrayIndexers);
                    }
                    else
                    {
                        // Keep this difference as it doesn't match any ChildAdded
                        remainingDifferences.Add(diff);
                    }
                }

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
        }

        // Remove PropertiesChanged that were fully consolidated, in reverse order
        foreach (var index in changesToRemove.OrderDescending())
        {
            _changes.RemoveAt(index);
        }
    }

    ChildAdded? FindMatchingChildAdded(List<ChildAdded> childAddedChanges, PropertyDifference diff)
    {
        // A PropertiesChanged targets a child if:
        // 1. The property path points to a child within an array
        // 2. The array indexers match a ChildAdded's key and array property
        if (diff.ArrayIndexers.IsEmpty)
        {
            return null;
        }

        foreach (var childAdded in childAddedChanges)
        {
            // Check if the array indexers match
            foreach (var indexer in diff.ArrayIndexers.All)
            {
                if (indexer.ArrayProperty == childAdded.ChildrenProperty &&
                    indexer.IdentifierProperty == childAdded.IdentifiedByProperty &&
                    Equals(indexer.Identifier, childAdded.Key))
                {
                    return childAdded;
                }
            }
        }

        return null;
    }

    void ConsolidateConflictingOperations()
    {
        // Find all arrays that have ChildAdded or ChildRemoved operations
        var arraysWithChildOperations = new HashSet<PropertyPath>();
        foreach (var change in _changes)
        {
            if (change is ChildAdded childAdded)
            {
                arraysWithChildOperations.Add(childAdded.ChildrenProperty);
            }
            else if (change is ChildRemoved childRemoved)
            {
                arraysWithChildOperations.Add(childRemoved.ChildrenProperty);
            }
        }

        // If there are arrays with child operations, filter out PropertiesChanged that set those arrays to empty
        if (arraysWithChildOperations.Count > 0)
        {
            var changesToUpdate = new List<(int Index, Change Change)>();

            for (var i = 0; i < _changes.Count; i++)
            {
                var change = _changes[i];
                if (change is PropertiesChanged<TTarget> propertiesChanged)
                {
                    // Filter out property differences that conflict with child operations
                    var nonConflictingDifferences = propertiesChanged.Differences
                        .Where(diff => !ShouldFilterPropertyDifference(diff, arraysWithChildOperations))
                        .ToList();

                    // Only keep the PropertiesChanged if there are non-conflicting differences
                    if (nonConflictingDifferences.Count != propertiesChanged.Differences.Count())
                    {
                        if (nonConflictingDifferences.Count != 0)
                        {
                            changesToUpdate.Add((i, new PropertiesChanged<TTarget>(
                                propertiesChanged.State,
                                nonConflictingDifferences)));
                        }
                        else
                        {
                            changesToUpdate.Add((i, null!));
                        }
                    }
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

    bool ShouldFilterPropertyDifference(PropertyDifference diff, HashSet<PropertyPath> arraysWithChildOperations)
    {
        // Check if this property difference is setting an array to empty
        if (diff.Changed is System.Collections.IEnumerable enumerable && enumerable.CountElements() == 0)
        {
            // Check if this array path matches any of the arrays with child operations
            foreach (var arrayPath in arraysWithChildOperations)
            {
                if (diff.PropertyPath.Equals(arrayPath))
                {
                    return true;
                }
            }
        }

        return false;
    }

    List<PropertyDifference> SetProperties(TTarget state, IEnumerable<PropertyMapper<TSource, TTarget>> propertyMappers, ArrayIndexers arrayIndexers)
    {
        var differences = new List<PropertyDifference>();

        foreach (var propertyMapper in propertyMappers)
        {
            var difference = propertyMapper(Incoming, state, arrayIndexers);
            if (difference.HasChanges())
            {
                differences.Add(difference);
            }
        }

        return differences;
    }

    void ApplyPropertyMappers(object target, IEnumerable<PropertyMapper<TSource, TTarget>> propertyMappers, ArrayIndexers arrayIndexers)
    {
        foreach (var propertyMapper in propertyMappers)
        {
            var difference = propertyMapper(Incoming, (TTarget)target, arrayIndexers);
            if (difference.HasChanges())
            {
                difference.PropertyPath.SetValue(target, difference.Changed!, difference.ArrayIndexers);
            }
        }
    }
}
