// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive;
using System.Reactive.Subjects;
using Cratis.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Security;

/// <summary>Maintains a leased snapshot off the request path.</summary>
/// <param name="grains">The authoritative grain factory.</param>
/// <param name="logger">The cache logger.</param>
/// <param name="time">The clock and timer provider.</param>
[IgnoreConvention]
public sealed class StoreEntitlementsCache(IGrainFactory grains, ILogger<StoreEntitlementsCache> logger, TimeProvider time) : IStoreEntitlementsCache, IDisposable
{
    static readonly TimeSpan _pollInterval = TimeSpan.FromSeconds(30);
    static readonly TimeSpan _lease = TimeSpan.FromMinutes(5);
    readonly Subject<Unit> _changes = new();
    readonly object _sync = new();
    volatile Published _current = new(StoreEntitlementsSnapshot.Empty, null);
    ITimer? _timer;

    /// <inheritdoc/>
    public StoreEntitlementsSnapshot Snapshot => _current.Snapshot;

    /// <inheritdoc/>
    public DateTimeOffset? LoadedAt => _current.LoadedAt;

    /// <inheritdoc/>
    public bool IsFresh => LoadedAt is { } loaded && time.GetUtcNow() - loaded <= _lease;

    /// <inheritdoc/>
    public IObservable<Unit> Changes => _changes;

    IStoreEntitlements Grain => grains.GetGrain<IStoreEntitlements>(IStoreEntitlements.Key);

    /// <inheritdoc/>
    public async Task Load()
    {
        await Refresh();
        _timer ??= time.CreateTimer(_ => _ = Poll(), null, _pollInterval, _pollInterval);
    }

    /// <summary>Refreshes the snapshot from its authoritative grain.</summary>
    /// <returns>An awaitable task.</returns>
    public async Task Refresh()
    {
        try { Publish(await Grain.GetSnapshot()); }
        catch (Exception) { logger.StoreEntitlementsRefreshFailed(); }
    }

    /// <inheritdoc/>
    public void Publish(StoreEntitlementsState state)
    {
        var snapshot = new StoreEntitlementsSnapshot(state);
        lock (_sync)
        {
            if (state.Version < _current.Snapshot.Version) return;
            _current = new(snapshot, time.GetUtcNow());
        }
        _changes.OnNext(Unit.Default);
    }

    /// <summary>Confirms the version or repairs a missed broadcast.</summary>
    /// <returns>An awaitable task.</returns>
    public async Task Poll()
    {
        try
        {
            var version = await Grain.GetVersion();
            lock (_sync)
            {
                if (_current.LoadedAt is not null && _current.Snapshot.Version == version)
                {
                    _current = new(_current.Snapshot, time.GetUtcNow());
                    return;
                }
            }
            await Refresh();
        }
        catch (Exception) { logger.StoreEntitlementsRefreshFailed(); }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        _timer?.Dispose();
        _changes.Dispose();
    }

    sealed record Published(StoreEntitlementsSnapshot Snapshot, DateTimeOffset? LoadedAt);
}
