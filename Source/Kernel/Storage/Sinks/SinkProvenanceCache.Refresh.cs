// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Storage.Sinks;

public sealed partial class SinkProvenanceCache
{
    /// <summary>
    /// Shares monotonic observations, refresh work and revocation for one database.
    /// </summary>
    /// <param name="owner">The silo cache and clock.</param>
    /// <param name="raw">The durable metadata access.</param>
    /// <param name="initial">The validated initial observation.</param>
    internal sealed class Entry(SinkProvenanceCache owner, ISinkProvenanceStorage raw, SinkProvenanceState initial) : IDisposable
    {
        readonly Lock _gate = new();
        readonly CancellationTokenSource _revoked = new();
        readonly Lock _revocationGate = new();
        bool _revocationDisposed;
        TaskCompletionSource _changed = NewSignal();
        SinkProvenanceState _state = initial;
        Task? _refresh;
        long _started;
        bool _valid;
        bool _disposed;
        SinkProvenanceLease? _lease;

        /// <summary>
        /// Gets the shared read lease.
        /// </summary>
        internal SinkProvenanceLease Lease
        {
            get
            {
                lock (_gate)
                {
                    ObjectDisposedException.ThrowIf(_disposed, this);
                    return _lease ??= new(this);
                }
            }
        }

        /// <summary>
        /// Gets the refresh work to drain before disposing the shared semaphore.
        /// </summary>
        internal Task InFlight
        {
            get
            {
                lock (_gate) return _refresh ?? Task.CompletedTask;
            }
        }

        /// <summary>
        /// Gets the revocation token captured when a lease is created.
        /// </summary>
        internal CancellationToken Revoked => _revoked.Token;

        /// <summary>
        /// Gets whether a restricted mark has been observed.
        /// </summary>
        internal bool IsRestricted
        {
            get
            {
                lock (_gate) return _state.Restricted;
            }
        }

        static TaskCompletionSource NewSignal() => new(TaskCreationOptions.RunContinuationsAsynchronously);

        /// <inheritdoc/>
        public void Dispose()
        {
            lock (_gate)
            {
                if (_disposed) return;
                _disposed = true;
                _valid = false;
                Pulse();
            }
            lock (_revocationGate)
            {
                _revocationDisposed = true;
                _revoked.Dispose();
            }
        }

        /// <summary>
        /// Reads current facts, refreshing an expired observation.
        /// </summary>
        /// <returns>The validated database state.</returns>
        internal async Task<SinkProvenanceState> ReadState()
        {
            bool refresh;
            lock (_gate) refresh = !Fresh();
            if (refresh) await Refresh();
            lock (_gate)
            {
                if (!_valid || _disposed) throw SinkProvenanceLease.UnavailableError();
                return _state;
            }
        }

        /// <summary>
        /// Obtains fresh authority for a one-shot read.
        /// </summary>
        /// <returns>The admission task.</returns>
        internal async Task Ensure()
        {
            bool refresh;
            lock (_gate)
            {
                if (_state.Restricted || _disposed) throw SinkProvenanceLease.UnavailableError();
                refresh = !Fresh();
            }
            if (refresh) await Refresh();
            Deliver(static () => { });
        }

        /// <summary>
        /// Holds a reader until clean authority returns or a mark is observed.
        /// </summary>
        /// <param name="cancellationToken">The reader cancellation.</param>
        /// <returns>The wait task.</returns>
        internal async Task WaitUntilReadable(CancellationToken cancellationToken)
        {
            while (true)
            {
                Task changed;
                lock (_gate)
                {
                    if (_state.Restricted || _disposed) throw SinkProvenanceLease.UnavailableError();
                    if (Readable()) return;
                    changed = _changed.Task;
                }

                // The shared poller owns recovery. Expiry alone must not end an observation or spin retries.
                await changed.WaitAsync(cancellationToken);
            }
        }

        /// <summary>
        /// Serializes final delivery with permission checks.
        /// </summary>
        /// <param name="deliver">The synchronous delivery.</param>
        internal void Deliver(Action deliver)
        {
            lock (_gate)
            {
                if (!Readable()) throw SinkProvenanceLease.UnavailableError();
                deliver();
            }
        }

        /// <summary>
        /// Publishes local denial before a durable mark is attempted.
        /// </summary>
        internal void Deny()
        {
            lock (_gate)
            {
                _state = _state with { Restricted = true };
                Pulse();
            }
            PublishRevocation();
        }

        /// <summary>
        /// Publishes a validated observation without clearing monotonic facts.
        /// </summary>
        /// <param name="state">The committed state.</param>
        /// <param name="started">The monotonic start of its metadata read.</param>
        internal void Observe(SinkProvenanceState state, long started)
        {
            state.Validate();
            bool revoke;
            lock (_gate)
            {
                if (_disposed) return;
                if (state.DatabaseId != _state.DatabaseId || (_state.IsComplete && !state.IsComplete))
                {
                    _valid = false;
                    Pulse();
                    throw SinkProvenanceLease.UnavailableError();
                }

                // Facts are monotonic even when observations or an explicit mark race a refresh.
                _state = state with { Restricted = _state.Restricted || state.Restricted };
                _started = started;
                _valid = true;
                revoke = _state.Restricted;
                Pulse();
            }
            if (revoke) PublishRevocation();
        }

        /// <summary>
        /// Starts at most one background refresh for this database.
        /// </summary>
        internal void ScheduleRefresh()
        {
            lock (_gate)
            {
                if (_disposed || _state.Restricted || _refresh is not null) return;
            }
            _ = ObserveRefresh();
        }

        async Task ObserveRefresh()
        {
            try
            {
                await Refresh();
            }
            catch (Exception exception)
            {
                owner._logger.LogDebug(exception, "Read-model metadata refresh is unavailable.");
            }
        }

        Task Refresh()
        {
            lock (_gate)
            {
                if (_disposed) return Task.FromException(SinkProvenanceLease.UnavailableError());
                if (_refresh is not null) return _refresh;
                var completion = NewSignal();
                _refresh = completion.Task;
                _ = RefreshCore(completion);
                return completion.Task;
            }
        }

        async Task RefreshCore(TaskCompletionSource completion)
        {
            var acquired = false;
            try
            {
                await owner._refreshSlots.WaitAsync().ConfigureAwait(false);
                acquired = true;
                lock (_gate) ObjectDisposedException.ThrowIf(_disposed, this);
                var started = owner._time.GetTimestamp();
                Observe(await raw.ReadState().ConfigureAwait(false), started);
                completion.TrySetResult();
            }
            catch (Exception exception)
            {
                lock (_gate)
                {
                    _valid = false;
                    Pulse();
                }
                completion.TrySetException(exception);
            }
            finally
            {
                if (acquired) owner._refreshSlots.Release();
                lock (_gate) _refresh = null;
            }
        }

        bool Readable() => !_disposed && _state.IsComplete && !_state.Restricted && Fresh();
        bool Fresh() => _valid && owner._time.GetElapsedTime(_started) < TimeSpan.FromSeconds(2);

        void PublishRevocation()
        {
            lock (_revocationGate)
            {
                if (!_revocationDisposed) _revoked.Cancel();
            }
        }

        void Pulse()
        {
            var changed = _changed;
            _changed = NewSignal();
            changed.TrySetResult();
        }
    }
}
