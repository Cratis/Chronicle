// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Cratis.Chronicle.ReadModels;

namespace Cratis.Chronicle.Reducers;

/// <summary>
/// Represents an implementation of <see cref="IReducerObservers"/>.
/// </summary>
public class ReducerObservers : IReducerObservers
{
    readonly ConcurrentDictionary<Type, IReducerWatcher> _watchersByReadModelType = new();

    /// <inheritdoc/>
    public IReducerWatcher<TReadModel> GetWatcher<TReadModel>()
    {
        var watcher = _watchersByReadModelType.GetOrAdd(typeof(TReadModel), _ => new ReducerWatcher<TReadModel>());
        return (IReducerWatcher<TReadModel>)watcher;
    }

    /// <inheritdoc/>
    public void NotifyChange<TReadModel>(EventStoreNamespaceName @namespace, ReadModelKey modelKey, TReadModel? readModel, bool removed)
    {
        if (_watchersByReadModelType.TryGetValue(typeof(TReadModel), out var watcher))
        {
            ((ReducerWatcher<TReadModel>)watcher).NotifyChange(@namespace, modelKey, readModel, removed);
        }
    }
}
