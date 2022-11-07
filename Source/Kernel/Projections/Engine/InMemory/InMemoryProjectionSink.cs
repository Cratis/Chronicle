// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Aksio.Cratis.Changes;
using Aksio.Cratis.Dynamic;
using Aksio.Cratis.Events.Store;
using Aksio.Cratis.Schemas;
using Aksio.Cratis.Types;

namespace Aksio.Cratis.Events.Projections.InMemory;

/// <summary>
/// Represents an implementation of <see cref="IProjectionSink"/> for working with projections in memory.
/// </summary>
public class InMemoryProjectionSink : IProjectionSink, IDisposable
{
    readonly Dictionary<object, ExpandoObject> _collection = new();
    readonly Dictionary<object, ExpandoObject> _rewindCollection = new();
    readonly Model _model;
    readonly ITypeFormats _typeFormats;
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
    public InMemoryProjectionSink(Model model, ITypeFormats typeFormats)
    {
        _model = model;
        _typeFormats = typeFormats;
    }

    /// <inheritdoc/>
    public Task<ExpandoObject?> FindOrDefault(Key key)
    {
        var collection = Collection;
        var keyValue = GetActualKeyValue(key);

        ExpandoObject modelInstance;
        if (collection.ContainsKey(keyValue))
        {
            modelInstance = collection[keyValue];
        }
        else
        {
            modelInstance = new ExpandoObject();
        }

        return Task.FromResult<ExpandoObject?>(modelInstance);
    }

    /// <inheritdoc/>
    public Task ApplyChanges(Key key, IChangeset<AppendedEvent, ExpandoObject> changeset)
    {
        var state = changeset.InitialState.Clone();
        var collection = Collection;

        if (changeset.HasBeenRemoved())
        {
            collection.Remove(key.Value);
            return Task.CompletedTask;
        }

        var keyValue = GetActualKeyValue(key);
        collection[keyValue] = ApplyActualChanges(changeset.Changes, state);

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
    public Task PrepareInitialRun()
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
        var targetType = _model.Schema.GetTargetTypeForPropertyPath("id", _typeFormats);
        if (targetType is not null)
        {
            return TypeConversion.Convert(targetType, key.Value);
        }

        if (key.Value.IsConcept())
        {
            return key.Value.GetConceptValue();
        }

        return key.Value;
    }

    ExpandoObject ApplyActualChanges(IEnumerable<Change> changes, ExpandoObject state)
    {
        foreach (var change in changes)
        {
            switch (change)
            {
                case PropertiesChanged<ExpandoObject>:
                case ChildAdded:
                    state = state.MergeWith((change.State as ExpandoObject)!);
                    break;

                case Joined joined:
                    state = ApplyActualChanges(joined.Changes, state);
                    break;

                case ResolvedJoin resolvedJoin:
                    state = ApplyActualChanges(resolvedJoin.Changes, state);
                    break;
            }
        }

        return state;
    }
}
