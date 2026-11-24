// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Cratis.DependencyInjection;

namespace Cratis.Chronicle.Storage.Sinks;

/// <summary>
/// Caches revocable observations by durable physical identity, independently of routing aliases.
/// </summary>
/// <param name="timeProvider">The monotonic clock and timer provider.</param>
[Singleton]
public sealed partial class SinkProvenanceCache(TimeProvider? timeProvider = null) : ISinkProvenanceCache, IDisposable, IAsyncDisposable
{
    readonly TimeProvider _time = timeProvider ?? TimeProvider.System;
    readonly ConcurrentDictionary<object, Handle> _routes = new();
    readonly ConcurrentDictionary<SinkProvenanceDatabaseId, Entry> _entries = new();
    readonly SemaphoreSlim _refreshSlots = new(4, 4);
    readonly Lock _lifetime = new();
    ITimer? _timer;
    bool _disposed;

    /// <inheritdoc/>
    public ISinkProvenanceStorage Bind(object route, Func<ISinkProvenanceStorage> create)
    {
        lock (_lifetime)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            _timer ??= _time.CreateTimer(Poll, null, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));
            return _routes.GetOrAdd(route, _ => new(this, create()));
        }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        lock (_lifetime)
        {
            if (_disposed) return;
            _disposed = true;
            _timer?.Dispose();
            foreach (var entry in _entries.Values) entry.Dispose();
        }
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        Dispose();
        var draining = _entries.Values.Select(_ => _.InFlight).Concat(_routes.Values.Select(_ => _.InFlight)).ToArray();
        try { await Task.WhenAll(draining).ConfigureAwait(false); }
        catch (Exception) { /* Disposal already denied publication; failed metadata reads grant nothing. */ }
    }

    void Poll(object? state)
    {
        foreach (var entry in _entries.Values) entry.ScheduleRefresh();
    }

    async Task<Entry> Initialize(ISinkProvenanceStorage storage)
    {
        SinkProvenanceState state;
        long started;
        await _refreshSlots.WaitAsync().ConfigureAwait(false);
        try
        {
            ObjectDisposedException.ThrowIf(Volatile.Read(ref _disposed), this);
            started = _time.GetTimestamp();
            state = (await storage.ReadState().ConfigureAwait(false)).Validate();
        }
        finally { _refreshSlots.Release(); }
        Entry entry;
        lock (_lifetime)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            entry = _entries.GetOrAdd(state.DatabaseId, _ => new(this, storage, state));
        }
        entry.Observe(state, started);
        return entry;
    }

    sealed class Handle(SinkProvenanceCache owner, ISinkProvenanceStorage raw) : ISinkProvenanceStorage
    {
        readonly Lock _gate = new();
        Task<Entry>? _entry;

        internal Task InFlight { get { lock (_gate) return (Task?)_entry ?? Task.CompletedTask; } }

        public async Task<SinkProvenanceState> ReadState() => await (await GetEntry()).ReadState();

        public async Task<SinkProvenanceLease> GetLease() => (await GetEntry()).Lease;

        public async Task<bool> IsQuarantined()
        {
            var entry = await GetEntry();
            if (entry.IsRestricted) return true;
            await entry.Ensure();
            return false;
        }

        public async Task MarkQuarantined()
        {
            var entry = await GetEntry();
            entry.Deny();
            await raw.MarkQuarantined();
        }

        public async Task<SinkProvenanceState> CompleteInventory(bool restrictedEvidence)
        {
            var entry = await GetEntry();
            var started = owner._time.GetTimestamp();
            var state = (await raw.CompleteInventory(restrictedEvidence)).Validate();
            entry.Observe(state, started);
            return state;
        }

        Task<Entry> GetEntry()
        {
            lock (_gate)
            {
                ObjectDisposedException.ThrowIf(Volatile.Read(ref owner._disposed), owner);
                if (_entry is null || _entry.IsFaulted || _entry.IsCanceled) _entry = owner.Initialize(raw);
                return _entry;
            }
        }
    }
}
