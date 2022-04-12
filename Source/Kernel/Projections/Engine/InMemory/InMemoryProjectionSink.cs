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
    /// <summary>
    /// Gets the identifier of the <see cref="InMemoryProjectionSink"/>.
    /// </summary>
    public static readonly ProjectionSinkTypeId ProjectionResultStoreTypeId = "8a23995d-da0b-4c4c-818b-f97992f26bbf";

    readonly Dictionary<object, ExpandoObject> _collection = new();
    readonly Dictionary<object, ExpandoObject> _rewindCollection = new();

    /// <inheritdoc/>
    public ProjectionSinkTypeId TypeId => ProjectionResultStoreTypeId;

    /// <inheritdoc/>
    public ProjectionSinkTypeName Name => "InMemory";

    /// <inheritdoc/>
    public Task<ExpandoObject> FindOrDefault(Key key)
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

        return Task.FromResult(modelInstance);
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

        foreach (var change in changeset.Changes)
        {
            state = state.OverwriteWith((change.State as ExpandoObject)!);
        }

        collection[key.Value] = state;

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

    Dictionary<object, ExpandoObject> GetCollection() => IsRewinding ? _rewindCollection : _collection;

    static bool IsRewinding => false;
}
