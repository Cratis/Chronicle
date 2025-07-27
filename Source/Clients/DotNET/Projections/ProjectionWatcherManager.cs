// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;

namespace Cratis.Chronicle.Projections;

/// <summary>
/// Represents an implementation of <see cref="IProjectionWatcherManager"/>.
/// </summary>
/// <param name="projectionWatcherFactory"><see cref="IProjectionWatcherFactory"/> for creating watchers.</param>
/// <param name="eventStore"><see cref="IEventStore"/> the manager is for.</param>
public class ProjectionWatcherManager(IProjectionWatcherFactory projectionWatcherFactory, IEventStore eventStore) : IProjectionWatcherManager
{
    readonly ConcurrentDictionary<Type, IProjectionWatcher> _watchers = [];

    /// <inheritdoc/>
    public IProjectionWatcher<TReadModel> GetWatcher<TReadModel>()
    {
        if (!_watchers.TryGetValue(typeof(TReadModel), out var watcher))
        {
            watcher = _watchers[typeof(TReadModel)] = projectionWatcherFactory.Create<TReadModel>(() => _watchers.TryRemove(typeof(TReadModel), out _));
            if (eventStore.Connection.Lifecycle.IsConnected)
            {
                watcher.Start();
            }
        }

        return (watcher as IProjectionWatcher<TReadModel>)!;
    }
}
