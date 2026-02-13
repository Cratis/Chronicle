// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;

namespace Cratis.Chronicle.ReadModels;

/// <summary>
/// Represents an implementation of <see cref="IReadModelWatcherManager"/>.
/// </summary>
/// <param name="readModelWatcherFactory"><see cref="IReadModelWatcherFactory"/> for creating watchers.</param>
public class ReadModelWatcherManager(IReadModelWatcherFactory readModelWatcherFactory) : IReadModelWatcherManager
{
    readonly ConcurrentDictionary<Type, IReadModelWatcher> _watchers = [];

    /// <inheritdoc/>
    public IReadModelWatcher<TReadModel> GetWatcher<TReadModel>()
    {
        if (!_watchers.TryGetValue(typeof(TReadModel), out var watcher))
        {
            watcher = _watchers[typeof(TReadModel)] = readModelWatcherFactory.Create<TReadModel>(() => _watchers.TryRemove(typeof(TReadModel), out _));

            // Always start the watcher immediately. If the connection is not established yet,
            // calling Start() will trigger the connection through Services access.
            // The OnConnected callback in ReadModelWatcher will handle re-starting if needed.
            watcher.Start();
        }

        return (watcher as IReadModelWatcher<TReadModel>)!;
    }
}
