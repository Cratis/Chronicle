// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Aksio.Changes;
using Aksio.Cratis.Events;
using Aksio.Cratis.Projections;
using Aksio.Dynamic;
using Aksio.Json;
using Aksio.Reflection;
using Aksio.Schemas;
using Aksio.Types;

namespace Aksio.Cratis.Kernel.Engines.Projections.InMemory;

/// <summary>
/// Represents an implementation of <see cref="IProjectionSink"/> for working with projections in memory.
/// </summary>
public class InMemoryProjectionSink : IProjectionSink, IDisposable
{
    readonly Dictionary<object, ExpandoObject> _collection = new();
    readonly Dictionary<object, ExpandoObject> _rewindCollection = new();
    readonly Model _model;
    readonly ITypeFormats _typeFormats;
    readonly IObjectsComparer _comparer;
    bool _isReplaying;

    /// <inheritdoc/>
    public ProjectionSinkTypeId TypeId => WellKnownProjectionSinkTypes.InMemory;

    /// <inheritdoc/>
    public ProjectionSinkTypeName Name => "InMemory";

    /// <summary>
    /// Gets the current collection for the sink represented as a key value of key to <see cref="ExpandoObject"/>.
    /// </summary>
    public IDictionary<object, ExpandoObject> Collection => _isReplaying ? _rewindCollection : _collection;

    /// <summary>
    /// Initializes a new instance of the <see cref="InMemoryProjectionSink"/> class.
    /// </summary>
    /// <param name="model">The target <see cref="Model"/>.</param>
    /// <param name="typeFormats">The <see cref="ITypeFormats"/> for resolving actual types from JSON schema.</param>
    /// <param name="comparer"><see cref="IObjectsComparer"/> used for complex comparisons of objects.</param>
    public InMemoryProjectionSink(
        Model model,
        ITypeFormats typeFormats,
        IObjectsComparer comparer)
    {
        _model = model;
        _typeFormats = typeFormats;
        _comparer = comparer;
    }

    /// <inheritdoc/>
    public Task<ExpandoObject?> FindOrDefault(Key key, bool isReplaying)
    {
        var collection = Collection;
        var keyValue = GetActualKeyValue(key);

        if (keyValue is ExpandoObject)
        {
            return Task.FromResult<ExpandoObject?>(collection.SingleOrDefault(kvp => _comparer.Equals(kvp.Key, keyValue, out _)).Value);
        }

        if (collection.ContainsKey(keyValue))
        {
            return Task.FromResult<ExpandoObject?>(collection[keyValue]);
        }

        return Task.FromResult<ExpandoObject?>(null);
    }

    /// <inheritdoc/>
    public Task ApplyChanges(Key key, IChangeset<AppendedEvent, ExpandoObject> changeset, bool isReplaying)
    {
        var state = changeset.InitialState.Clone();
        var collection = Collection;

        if (changeset.HasBeenRemoved())
        {
            collection.Remove(key.Value);
            return Task.CompletedTask;
        }

        var keyValue = GetActualKeyValue(key);
        collection[keyValue] = ApplyActualChanges(key, changeset.Changes, state);

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task BeginReplay()
    {
        _isReplaying = true;
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task EndReplay()
    {
        _isReplaying = false;
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task PrepareInitialRun(bool isReplaying)
    {
        Collection.Clear();
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }

    object GetActualKeyValue(Key key)
    {
        if (key.Value is ExpandoObject)
        {
            return key.Value;
        }

        var targetType = _model.Schema.GetTargetTypeForPropertyPath("id", _typeFormats);
        if (targetType is not null)
        {
            return TypeConversion.Convert(targetType, key.Value);
        }

        if (key.Value.IsConcept())
        {
            return key.Value.GetConceptValue();
        }

        if (!key.Value.GetType().IsAPrimitiveType())
        {
            return key.Value.AsExpandoObject(true);
        }

        return key.Value;
    }

    ExpandoObject ApplyActualChanges(Key key, IEnumerable<Change> changes, ExpandoObject state)
    {
        foreach (var change in changes)
        {
            switch (change)
            {
                case PropertiesChanged<ExpandoObject>:
                    state = state.MergeWith((change.State as ExpandoObject)!);
                    break;

                case ChildAdded childAdded:
                    var collection = state.EnsureCollection<ExpandoObject, object>(childAdded.ChildrenProperty, key.ArrayIndexers);
                    collection.Add(childAdded.State);
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
