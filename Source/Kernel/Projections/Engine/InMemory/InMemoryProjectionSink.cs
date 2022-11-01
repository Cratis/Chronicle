// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Aksio.Cratis.Changes;
using Aksio.Cratis.Dynamic;
using Aksio.Cratis.Events.Store;

namespace Aksio.Cratis.Events.Projections.InMemory;

/// <summary>
/// Represents an implementation of <see cref="IProjectionSink"/> for working with projections in memory.
/// </summary>
public class InMemoryProjectionSink : IProjectionSink, IDisposable
{
    readonly Dictionary<object, ExpandoObject> _collection = new();
    readonly Dictionary<object, ExpandoObject> _rewindCollection = new();
    bool _isReplaying;

    /// <inheritdoc/>
    public ProjectionSinkTypeId TypeId => WellKnownProjectionSinkTypes.InMemory;

    /// <inheritdoc/>
    public ProjectionSinkTypeName Name => "InMemory";

    /// <inheritdoc/>
    public Task<ExpandoObject?> FindOrDefault(Key key)
    {
        var collection = GetCollection();

        ExpandoObject modelInstance;
        if (collection.ContainsKey(key.Value))
        {
            modelInstance = collection[key.Value];
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
        var collection = GetCollection();

        if (changeset.HasBeenRemoved())
        {
            collection.Remove(key.Value);
            return Task.CompletedTask;
        }

        collection[key.Value] = ApplyActualChanges(changeset.Changes, state);

        return Task.CompletedTask;
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
        GetCollection().Clear();
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Gets the current collection for the sink represented as a key value of key to <see cref="ExpandoObject"/>.
    /// </summary>
    /// <returns>The collection.</returns>
    public Dictionary<object, ExpandoObject> GetCollection() => _isReplaying ? _rewindCollection : _collection;
}
