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

    readonly Dictionary<string, ExpandoObject> _collection = new();
    readonly Dictionary<string, ExpandoObject> _rewindCollection = new();
    bool _isReplaying;

    /// <inheritdoc/>
    public ProjectionSinkTypeId TypeId => ProjectionResultStoreTypeId;

    /// <inheritdoc/>
    public ProjectionSinkTypeName Name => "InMemory";

    /// <inheritdoc/>
    public Task<ExpandoObject> FindOrDefault(Key key)
    {
        var collection = GetCollection();

        ExpandoObject modelInstance;
        if (collection.ContainsKey(key.Value.ToString()!))
        {
            modelInstance = collection[key.Value.ToString()!];
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
            collection.Remove(key.Value.ToString()!);
            return Task.CompletedTask;
        }

        foreach (var change in changeset.Changes)
        {
            state = state.OverwriteWith((change.State as ExpandoObject)!);
        }

        collection[key.Value.ToString()!] = state;

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
        GetCollection().Clear();
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }

    Dictionary<string, ExpandoObject> GetCollection() => _isReplaying ? _rewindCollection : _collection;
}
