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
/// <param name="parent">Optional parent <see cref="IChangeset{TSource, TTarget}"/> if any.</param>
public partial class Changeset<TSource, TTarget>(IObjectComparer comparer, TSource incoming, TTarget initialState, Changeset<TSource, TTarget>? parent = null) : IChangeset<TSource, TTarget>
{
    readonly List<Change> _changes = [];
    readonly List<ResolvedJoinChangeset> _resolvedJoinChangesets = [];
    readonly List<ChildAdded> _consolidatedIntoChildren = [];
    TTarget _initialState = initialState;
    bool _hasChildCollectionOperations;

    /// <inheritdoc/>
    public TSource Incoming { get; set; } = incoming;

    /// <inheritdoc/>
    public TTarget InitialState
    {
        get
        {
            return _initialState;
        }
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
        if (change is ChildAdded or ChildRemoved or ChildRemovedFromAll)
        {
            _hasChildCollectionOperations = true;
        }

        ApplyToConsolidatedChildren(change);
        Consolidate();
    }

    /// <inheritdoc/>
    public void SetProperties(IEnumerable<PropertyMapper<TSource, TTarget>> propertyMappers, ArrayIndexers arrayIndexers)
    {
        var workingState = CurrentState.Clone()!;
        var differences = SetProperties(workingState, propertyMappers, arrayIndexers);

        if (differences.Count == 0)
        {
            return;
        }

        Add(new PropertiesChanged<TTarget>(workingState, differences));
        SortChildrenAddedInThisChangeset(workingState);
        CurrentState = workingState;
    }

    /// <summary>
    /// Replace the whole state with a state computed outside the changeset, recording the differences that lead to it.
    /// </summary>
    /// <remarks>
    /// For a producer that computes the next state itself rather than through property mappers - a reducer. The state
    /// becomes <see cref="CurrentState"/>, because a sink in bulk mode keeps <see cref="CurrentState"/> as the instance
    /// the next batch for the same key starts from; leaving it at the initial state would make that batch start over.
    /// </remarks>
    /// <param name="state">The complete state after the change.</param>
    /// <param name="differences">The differences between the current state and <paramref name="state"/>.</param>
    public void ReplaceState(TTarget state, IEnumerable<PropertyDifference> differences)
    {
        Add(new PropertiesChanged<TTarget>(state!, differences));
        CurrentState = state;
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
        var resolvedJoin = new ResolvedJoin(workingState, key, onProperty, arrayIndexers, childChangeset.Changes, incoming);
        _resolvedJoinChangesets.Add(new(resolvedJoin, childChangeset));
        Add(resolvedJoin);
        CurrentState = workingState;
        return childChangeset;
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

    static object? ValuesOfChild(object? item, IEnumerable<ChildAdded> childrenAdded)
    {
        if (item is null)
        {
            return null;
        }

        foreach (var childAdded in childrenAdded)
        {
            if (ReferenceEquals(item, childAdded.Child))
            {
                return item;
            }

            if (childAdded.IdentifiedByProperty.IsSet &&
                ChildKeyComparison.IsSameKey(childAdded.IdentifiedByProperty.GetValue(item, ArrayIndexers.NoIndexers), childAdded.Key))
            {
                return childAdded.Child;
            }
        }

        return item;
    }

    /// <summary>
    /// A child added within this changeset reaches a sink sorting on insert with its final values. When a later change in
    /// the same changeset sets a value it is sorted by, its collection is ordered again so the state keeps the stored order.
    /// </summary>
    /// <param name="state">The state holding the collections to order.</param>
    void SortChildrenAddedInThisChangeset(TTarget state)
    {
        if (!_hasChildCollectionOperations || state is null)
        {
            return;
        }

        var sorted = _changes.OfType<ChildAdded>().Where(_ => _.Ordering?.Placement == ChildrenPlacement.Sorted && !_.Ordering.IsAppend()).ToArray();
        foreach (var childAdded in sorted)
        {
            // A sink stores the child the change carries, so its values decide where a child added here is placed.
            state.EnsureCollection<TTarget, object>(childAdded.ChildrenProperty, childAdded.ArrayIndexers)
                .SortChildren(childAdded.Ordering, childAdded.IdentifiedByProperty, item => ValuesOfChild(item, sorted.Where(_ => _.ChildrenProperty == childAdded.ChildrenProperty)));
        }
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

    sealed record PropertyDifferenceInChange(int ChangeIndex, PropertyDifference Difference);

    sealed record ResolvedJoinChangeset(ResolvedJoin Join, Changeset<TSource, TTarget> Changeset);
}
