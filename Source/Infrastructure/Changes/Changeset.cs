// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Objects;
using Cratis.Chronicle.Properties;

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
public class Changeset<TSource, TTarget>(IObjectComparer comparer, TSource incoming, TTarget initialState) : IChangeset<TSource, TTarget>
{
    readonly List<Change> _changes = [];

    /// <inheritdoc/>
    public TSource Incoming { get; } = incoming;

    /// <inheritdoc/>
    public TTarget InitialState { get; } = initialState;

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
    }

    /// <inheritdoc/>
    public void SetProperties(IEnumerable<PropertyMapper<TSource, TTarget>> propertyMappers, ArrayIndexers arrayIndexers)
    {
        var workingState = CurrentState.Clone()!;
        SetProperties(workingState, propertyMappers, arrayIndexers);

        if (!comparer.Equals(CurrentState, workingState, out var differences))
        {
            Add(new PropertiesChanged<TTarget>(workingState, differences));
        }

        CurrentState = workingState;
    }

    /// <inheritdoc/>
    public void Join(PropertyPath onProperty, object key, ArrayIndexers arrayIndexers)
    {
        var workingState = InitialState.Clone()!;
        Add(new Joined(workingState, key, onProperty, arrayIndexers, Changes));
        CurrentState = workingState;
    }

    /// <inheritdoc/>
    public void ResolvedJoin(PropertyPath onProperty, object key, TSource incoming, ArrayIndexers arrayIndexers)
    {
        var workingState = CurrentState.Clone()!;
        Add(new ResolvedJoin(workingState, key, onProperty, arrayIndexers, Changes));
        CurrentState = workingState;
    }

    /// <inheritdoc/>
    public void AddChild(PropertyPath childrenProperty, object child)
    {
        Add(new ChildAdded(child, childrenProperty, PropertyPath.Root, null!));
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
        var workingState = CurrentState.Clone()!;
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
                    // TODO: Should support non ref/class type objects as children.
                    item = new TChild();
                    identifiedByProperty.SetValue(item, key, ArrayIndexers.NoIndexers);
                }
            }
        }
        else
        {
            // TODO: Should support non ref/class type objects as children.
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
            Add(new ChildAdded(item, childrenProperty, identifiedByProperty, key!));
        }

        CurrentState = workingState;
    }

    /// <inheritdoc/>
    public void Remove()
    {
        Add(new Removed(CurrentState.Clone()!));
    }

    /// <inheritdoc/>
    public void RemoveChild()
    {
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

    void SetProperties(TTarget state, IEnumerable<PropertyMapper<TSource, TTarget>> propertyMappers, ArrayIndexers arrayIndexers)
    {
        foreach (var propertyMapper in propertyMappers)
        {
            propertyMapper(Incoming, state, arrayIndexers);
        }
    }
}
