// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Storage.Sql.Sinks;

/// <summary>
/// A changeset over a fixed array of changes. The sink reads only <see cref="Changes"/> and <see cref="HasBeenRemoved"/>, so
/// everything else throws. It answers <see cref="HasBeenRemoved"/> and <see cref="HasJoined"/> as the substitute it replaces did (false), so
/// the recorded command fingerprints stay valid, and it allocates no proxy.
/// </summary>
/// <param name="_changes">The changes.</param>
internal sealed class FixedChangeset(Change[] _changes) : IChangeset<AppendedEvent, ExpandoObject>
{
    /// <inheritdoc/>
    public AppendedEvent Incoming
    {
        get => throw new NotSupportedException();
        set => throw new NotSupportedException();
    }

    /// <inheritdoc/>
    public ExpandoObject InitialState
    {
        get => throw new NotSupportedException();
        set => throw new NotSupportedException();
    }

    /// <inheritdoc/>
    public ExpandoObject CurrentState => throw new NotSupportedException();

    /// <inheritdoc/>
    public IEnumerable<Change> Changes => _changes;

    /// <inheritdoc/>
    public bool HasChanges => _changes.Length > 0;

    /// <inheritdoc/>
    public bool HasBeenRemoved() => false;

    /// <inheritdoc/>
    public bool HasJoined() => false;

    /// <inheritdoc/>
    public bool HasRemoved() => false;

    /// <inheritdoc/>
    public void Add(Change change) => throw new NotSupportedException();

    /// <inheritdoc/>
    public void SetProperties(IEnumerable<PropertyMapper<AppendedEvent, ExpandoObject>> propertyMappers, ArrayIndexers arrayIndexers) => throw new NotSupportedException();

    /// <inheritdoc/>
    public IChangeset<AppendedEvent, ExpandoObject> Join(PropertyPath onProperty, object key, ArrayIndexers arrayIndexers) => throw new NotSupportedException();

    /// <inheritdoc/>
    public IChangeset<AppendedEvent, ExpandoObject> ResolvedJoin(PropertyPath onProperty, object key, AppendedEvent incoming, ArrayIndexers arrayIndexers) => throw new NotSupportedException();

    /// <inheritdoc/>
    public void AddChild(PropertyPath childrenProperty, object child, ChildrenOrdering? ordering = default) => throw new NotSupportedException();

    /// <inheritdoc/>
    public void AddChild<TChild>(PropertyPath childrenProperty, PropertyPath identifiedByProperty, object key, IEnumerable<PropertyMapper<AppendedEvent, ExpandoObject>> propertyMappers, ArrayIndexers arrayIndexers, ExpandoObject? childInitialState = default, ChildrenOrdering? ordering = default)
        where TChild : new() => throw new NotSupportedException();

    /// <inheritdoc/>
    public void Remove() => throw new NotSupportedException();

    /// <inheritdoc/>
    public void ClearNested(PropertyPath nestedProperty, ArrayIndexers arrayIndexers) => throw new NotSupportedException();

    /// <inheritdoc/>
    public void RemoveChild(PropertyPath childrenProperty, PropertyPath identifiedByProperty, object key, ArrayIndexers arrayIndexers) => throw new NotSupportedException();

    /// <inheritdoc/>
    public void RemoveChildFromAll(PropertyPath childrenProperty, PropertyPath identifiedByProperty, object key, ArrayIndexers arrayIndexers) => throw new NotSupportedException();

    /// <inheritdoc/>
    public bool HasChildBeenAddedWithKey(PropertyPath childrenProperty, object key) => throw new NotSupportedException();

    /// <inheritdoc/>
    public TChild GetChildByKey<TChild>(object key) => throw new NotSupportedException();
}
