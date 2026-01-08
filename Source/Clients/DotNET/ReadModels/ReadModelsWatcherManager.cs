// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;

namespace Cratis.Chronicle.ReadModels;

/// <summary>
/// Represents an implementation of <see cref="IReadModelsWatcherManager"/>.
/// </summary>
/// <param name="readModelsWatcherFactory"><see cref="IReadModelsWatcherFactory"/> for creating watchers.</param>
/// <param name="eventStore"><see cref="IEventStore"/> the manager is for.</param>
public class ReadModelsWatcherManager(IReadModelsWatcherFactory readModelsWatcherFactory, IEventStore eventStore) : IReadModelsWatcherManager
{
    readonly ConcurrentDictionary<Type, IReadModelsWatcher> _watchers = [];

    /// <inheritdoc/>
    public IReadModelsWatcher<TReadModel> GetWatcher<TReadModel>()
    {
        if (!_watchers.TryGetValue(typeof(TReadModel), out var watcher))
        {
            watcher = _watchers[typeof(TReadModel)] = readModelsWatcherFactory.Create<TReadModel>(() => _watchers.TryRemove(typeof(TReadModel), out _));
            if (eventStore.Connection.Lifecycle.IsConnected)
            {
                watcher.Start();
            }
        }

        return (watcher as IReadModelsWatcher<TReadModel>)!;
    }
}
