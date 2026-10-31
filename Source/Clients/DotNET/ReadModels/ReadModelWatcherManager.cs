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
    readonly object _sync = new();
    bool _released;

    /// <inheritdoc/>
    public IReadModelWatcher<TReadModel> GetWatcher<TReadModel>()
    {
        IReadModelWatcher? watcher;
        var created = false;
        lock (_sync)
        {
            ThrowIfReleased();
            if (!_watchers.TryGetValue(typeof(TReadModel), out watcher))
            {
                watcher = _watchers[typeof(TReadModel)] = readModelWatcherFactory.Create<TReadModel>(() => _watchers.TryRemove(typeof(TReadModel), out _));
                created = true;
            }
        }

        if (created)
        {
            // Always start the watcher immediately. If the connection is not established yet,
            // calling Start() will trigger the connection through Services access, so it never runs under the lock.
            // The OnConnected callback in ReadModelWatcher will handle re-starting if needed.
            watcher.Start();

            lock (_sync)
            {
                if (_released)
                {
                    watcher.Stop();
                    ThrowIfReleased();
                }
            }
        }

        return (watcher as IReadModelWatcher<TReadModel>)!;
    }

    /// <summary>
    /// Releases the watchers of an event store whose creation failed and that no caller holds. Stops every watcher,
    /// which closes its server stream and its connection lifecycle subscriptions, and refuses new ones.
    /// </summary>
    internal void Release()
    {
        IReadModelWatcher[] watchers;
        lock (_sync)
        {
            _released = true;
            watchers = [.. _watchers.Values];
        }

        foreach (var watcher in watchers)
        {
            watcher.Stop();
        }
    }

    void ThrowIfReleased() => ObjectDisposedException.ThrowIf(_released, this);
}
