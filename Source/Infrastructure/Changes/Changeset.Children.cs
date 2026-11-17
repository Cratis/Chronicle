// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Objects;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Changes;

public partial class Changeset<TSource, TTarget>
{
    /// <inheritdoc/>
    public void AddChild(PropertyPath childrenProperty, object child, ChildrenOrdering? ordering = default)
    {
        var workingState = CurrentState.Clone()!;
        var items = workingState.EnsureCollection<TTarget, object>(childrenProperty, ArrayIndexers.NoIndexers);
        items.InsertChild(child, ordering);
        Add(new ChildAdded(child, childrenProperty, PropertyPath.Root, null!, ArrayIndexers.NoIndexers, ordering));
        CurrentState = workingState;
    }

    /// <inheritdoc/>
    public void AddChild<TChild>(
        PropertyPath childrenProperty,
        PropertyPath identifiedByProperty,
        object key,
        IEnumerable<PropertyMapper<TSource, TTarget>> propertyMappers,
        ArrayIndexers arrayIndexers,
        TTarget? childInitialState = default,
        ChildrenOrdering? ordering = default)
        where TChild : new()
    {
        var workingState = CurrentState.Clone();
        var addition = new ChildAddition(childrenProperty, identifiedByProperty, key, arrayIndexers, ordering);

        // Check if there's already a ChildAdded change that matches this child in the same parent context
        var parentKey = arrayIndexers.All.First().Identifier;
        var existingChildAdded = _changes
            .OfType<ChildAdded>()
            .FirstOrDefault(_ => Equals(_.Key.ToString(), parentKey.ToString()));

        if (existingChildAdded is not null)
        {
            UpdateExistingChildAdded(workingState, existingChildAdded, addition, propertyMappers);
            return;
        }

        var items = workingState.EnsureCollection<TTarget, object>(childrenProperty, arrayIndexers);
        var (item, itemIndexers) = CreateChild<TChild>(items, addition, childInitialState);
        if (item is not null)
        {
            AppendChild(workingState, items, item, addition with { ArrayIndexers = itemIndexers }, propertyMappers);
        }

        CurrentState = workingState;
    }

    /// <inheritdoc/>
    public void Remove()
    {
        Add(new Removed(CurrentState.Clone()!));
    }

    /// <inheritdoc/>
    public void ClearNested(PropertyPath nestedProperty, ArrayIndexers arrayIndexers)
    {
        var workingState = CurrentState.Clone();
        NestedPropertyClearer.Clear(workingState, nestedProperty, arrayIndexers);

        Add(new NestedCleared(nestedProperty, arrayIndexers));
        CurrentState = workingState;
    }

    /// <inheritdoc/>
    public void RemoveChild(
        PropertyPath childrenProperty,
        PropertyPath identifiedByProperty,
        object key,
        ArrayIndexers arrayIndexers)
    {
        var workingState = CurrentState.Clone();
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

    static (object? Item, ArrayIndexers ArrayIndexers) CreateChild<TChild>(ICollection<object> items, ChildAddition addition, TTarget? childInitialState)
        where TChild : new()
    {
        if (!addition.IdentifiedByProperty.IsSet)
        {
            var indexers = new ArrayIndexers(
            [
                addition.ArrayIndexers.All.First() with { Identifier = items.Count }
            ]);
            return (NewChild<TChild>(childInitialState), indexers);
        }

        return (CreateIdentifiedChild<TChild>(items, addition, childInitialState), addition.ArrayIndexers);
    }

    static object? CreateIdentifiedChild<TChild>(ICollection<object> items, ChildAddition addition, TTarget? childInitialState)
        where TChild : new()
    {
        if (items.Contains(addition.IdentifiedByProperty, addition.Key))
        {
            return null;
        }

        // If the identified property is root, we want to add the item directly. That means
        // the object is identified by itself.
        if (addition.IdentifiedByProperty.IsRoot)
        {
            return addition.Key;
        }

        var item = NewChild<TChild>(childInitialState);
        addition.IdentifiedByProperty.SetValue(item!, addition.Key, ArrayIndexers.NoIndexers);
        return item;
    }

    static object? NewChild<TChild>(TTarget? childInitialState)
        where TChild : new() =>
        childInitialState is not null ? childInitialState.Clone() : new TChild();

    void UpdateExistingChildAdded(
        TTarget workingState,
        ChildAdded existingChildAdded,
        ChildAddition addition,
        IEnumerable<PropertyMapper<TSource, TTarget>> propertyMappers)
    {
        // Optimize by updating the existing child in-memory instead of creating a new change
        ApplyPropertyMappers(workingState!, propertyMappers, addition.ArrayIndexers);

        var existingCollection = workingState.EnsureCollection<TTarget, object>(
            existingChildAdded.ChildrenProperty,
            existingChildAdded.ArrayIndexers);
        var childToUpdate = existingCollection.FindByKey(existingChildAdded.IdentifiedByProperty, existingChildAdded.Key);
        if (childToUpdate is null)
        {
            return;
        }

        // The child was added within this changeset, so a sink sorting on insert sees its final values - order it
        // again by them so the engine's state keeps the order the sinks store.
        existingCollection.SortChildren(existingChildAdded.Ordering, existingChildAdded.IdentifiedByProperty);

        // Replace the existing ChildAdded with a new one containing the updated child
        var index = _changes.IndexOf(existingChildAdded);
        _changes[index] = new ChildAdded(
            childToUpdate,
            existingChildAdded.ChildrenProperty,
            existingChildAdded.IdentifiedByProperty,
            existingChildAdded.Key,
            existingChildAdded.ArrayIndexers,
            existingChildAdded.Ordering);

        ReplaceInCurrentState(addition, childToUpdate);
        CurrentState = workingState;
    }

    void ReplaceInCurrentState(ChildAddition addition, object child)
    {
        // Update the item in the current state's collection
        var collection = CurrentState.EnsureCollection<TTarget, object>(addition.ChildrenProperty, addition.ArrayIndexers);
        var existingItem = collection.FindByKey(addition.IdentifiedByProperty, addition.Key);
        if (existingItem is not null && collection is IList<object> list)
        {
            var itemIndex = list.IndexOf(existingItem);
            list[itemIndex] = child;
        }
    }

    void AppendChild(
        TTarget workingState,
        ICollection<object> items,
        object item,
        ChildAddition addition,
        IEnumerable<PropertyMapper<TSource, TTarget>> propertyMappers)
    {
        // Appended first so an unidentified child is addressed by its appended index while it is populated, and
        // placed afterwards so a sorted collection is ordered on the values the child was populated with.
        items.Add(item);
        foreach (var difference in SetProperties(workingState, propertyMappers, addition.ArrayIndexers))
        {
            difference.ApplyToNewChild(item, addition.ChildrenProperty, addition.ArrayIndexers);
        }

        items.PlaceAppendedChild(addition.Ordering, addition.IdentifiedByProperty);
        Add(new ChildAdded(item, addition.ChildrenProperty, addition.IdentifiedByProperty, addition.Key, addition.ArrayIndexers, addition.Ordering));
    }

    void ApplyPropertyMappers(object target, IEnumerable<PropertyMapper<TSource, TTarget>> propertyMappers, ArrayIndexers arrayIndexers)
    {
        foreach (var propertyMapper in propertyMappers)
        {
            var difference = propertyMapper(Incoming, (TTarget)target, arrayIndexers);
            if (difference.HasChanges())
            {
                difference.ApplyTo(target);
            }
        }
    }

    sealed record ChildAddition(
        PropertyPath ChildrenProperty,
        PropertyPath IdentifiedByProperty,
        object Key,
        ArrayIndexers ArrayIndexers,
        ChildrenOrdering? Ordering);
}
