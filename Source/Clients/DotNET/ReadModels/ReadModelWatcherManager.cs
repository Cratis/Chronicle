// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;

namespace Cratis.Chronicle.ReadModels;

/// <summary>
/// Represents an implementation of <see cref="IReadModelWatcherManager"/>.
/// </summary>
/// <param name="readModelWatcherFactory"><see cref="IReadModelWatcherFactory"/> for creating watchers.</param>
/// <param name="eventStore"><see cref="IEventStore"/> the manager is for.</param>
public class ReadModelWatcherManager(IReadModelWatcherFactory readModelWatcherFactory, IEventStore eventStore) : IReadModelWatcherManager
{
    readonly ConcurrentDictionary<Type, IReadModelWatcher> _watchers = [];

    /// <inheritdoc/>
    public IReadModelWatcher<TReadModel> GetWatcher<TReadModel>()
    {
        if (!_watchers.TryGetValue(typeof(TReadModel), out var watcher))
        {
            watcher = _watchers[typeof(TReadModel)] = readModelWatcherFactory.Create<TReadModel>(() => _watchers.TryRemove(typeof(TReadModel), out _));
            if (eventStore.Connection.Lifecycle.IsConnected)
            {
                watcher.Start();
            }
        }

        return (watcher as IReadModelWatcher<TReadModel>)!;
    }
}
