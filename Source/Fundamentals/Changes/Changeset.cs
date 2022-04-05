// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Objects;
using Aksio.Cratis.Properties;

namespace Aksio.Cratis.Changes;

/// <summary>
/// Represents an implementation of <see cref="IChangeset{TSource, TTarget}"/>.
/// </summary>
/// <typeparam name="TSource">Type of the source object we are working from.</typeparam>
/// <typeparam name="TTarget">Type of target object we are applying changes to.</typeparam>
public class Changeset<TSource, TTarget> : IChangeset<TSource, TTarget>
{
    readonly List<Change> _changes = new();
    readonly IObjectsComparer _comparer;

    /// <inheritdoc/>
    public TSource Incoming { get; }

    /// <inheritdoc/>
    public TTarget InitialState { get; }

    /// <inheritdoc/>
    public IEnumerable<Change> Changes => _changes;

    /// <inheritdoc/>
    public bool HasChanges => _changes.Count > 0;

    /// <summary>
    /// Initializes a new instance of <see cref="Changeset{TSource, TTarget}"/>.
    /// </summary>
    /// <param name="comparer"><see cref="IObjectsComparer"/> to compare objects with.</param>
    /// <param name="incoming"><see cref="Incoming"/> that the <see cref="Changeset{TSource, TTarget}"/> is for.</param>
    /// <param name="initialState">The initial state before any changes are applied.</param>
    public Changeset(IObjectsComparer comparer, TSource incoming, TTarget initialState)
    {
        _comparer = comparer;
        Incoming = incoming;
        InitialState = initialState;
    }

    /// <inheritdoc/>
    public void Add(Change change)
    {
        _changes.Add(change);
    }

    /// <inheritdoc/>
    public void SetProperties(IEnumerable<PropertyMapper<TSource, TTarget>> propertyMappers, IArrayIndexers arrayIndexers)
    {
        var workingState = InitialState.Clone()!;
        SetProperties(workingState, propertyMappers, arrayIndexers);

        if (!_comparer.Equals(InitialState, workingState, out var differences))
        {
            Add(new PropertiesChanged<TTarget>(workingState, differences));
        }
    }

    /// <inheritdoc/>
    public void AddChild<TChild>(
        PropertyPath childrenProperty,
        PropertyPath identifiedByProperty,
        object key,
        IEnumerable<PropertyMapper<TSource, TTarget>> propertyMappers,
        IArrayIndexers arrayIndexers)
        where TChild : new()
    {
        var workingState = InitialState.Clone()!;
        var items = workingState.EnsureCollection<TTarget, TChild>(childrenProperty, arrayIndexers);

        if (!items.Contains(identifiedByProperty, key))
        {
            var item = new TChild();
            identifiedByProperty.SetValue(item, key, ArrayIndexers.NoIndexers);
            ((IList<TChild>)items).Add(item);
            SetProperties(workingState, propertyMappers, arrayIndexers);
            Add(new ChildAdded(item, childrenProperty, identifiedByProperty, key!));
        }
    }

    /// <inheritdoc/>
    public void Remove()
    {
        Add(new Removed(InitialState.Clone()!));
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

    void SetProperties(TTarget state, IEnumerable<PropertyMapper<TSource, TTarget>> propertyMappers, IArrayIndexers arrayIndexers)
    {
        foreach (var propertyMapper in propertyMappers)
        {
            propertyMapper(Incoming, state, arrayIndexers);
        }
    }
}
