// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sinks;

public sealed partial class SinkProvenanceCache
{
    /// <summary>
    /// Shares monotonic observations, refresh work and revocation for one database.
    /// </summary>
    /// <param name="owner">The silo cache and clock.</param>
    /// <param name="databaseId">The immutable database identity shared by the routes.</param>
    internal sealed partial class Entry(SinkProvenanceCache owner, SinkProvenanceDatabaseId databaseId) : IDisposable
    {
        readonly Lock _gate = new();
        readonly SinkProvenanceDatabaseId _databaseId = databaseId;
        readonly CancellationTokenSource _revoked = new();
        readonly Lock _revocationGate = new();
        bool _revocationDisposed;
        Task? _notification;
        TaskCompletionSource _changed = NewSignal();
        SinkProvenanceState _state = new(databaseId, 0, false);
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
        /// Gets refresh and subscriber notification work to drain before disposal.
        /// </summary>
        internal Task InFlight
        {
            get
            {
                Task refresh;
                lock (_gate) refresh = _refresh ?? Task.CompletedTask;
                lock (_revocationGate) return Task.WhenAll(refresh, _notification ?? Task.CompletedTask);
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
                _notification = DisposeRevocation(_notification ?? Task.CompletedTask);
            }
        }

        /// <summary>
        /// Reads current facts, refreshing an expired observation.
        /// </summary>
        /// <returns>The validated database state.</returns>
        /// <exception cref="InvalidOperationException">The metadata is not currently available.</exception>
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
        /// <exception cref="InvalidOperationException">The lease cannot authorize a read.</exception>
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
        /// <exception cref="InvalidOperationException">A mark was observed or the cache was disposed.</exception>
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
        /// <exception cref="InvalidOperationException">The lease cannot authorize delivery.</exception>
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
            lock (_gate) LatchRestriction();
            PublishRevocation();
        }

        /// <summary>
        /// Starts at most one background refresh for this database.
        /// </summary>
        internal void ScheduleRefresh()
        {
            lock (_gate)
            {
                if (!CanRefresh()) return;
            }
            _ = ObserveRefresh();
        }

        static TaskCompletionSource NewSignal() => new(TaskCreationOptions.RunContinuationsAsynchronously);

        bool CanRefresh() => !_disposed && !_state.Restricted && _refresh is null && _stores.Count != 0;

        async Task ObserveRefresh()
        {
            try
            {
                await Refresh();
            }
            catch (Exception exception)
            {
                owner._logger.RefreshFailed(exception);
            }
        }

        Task Refresh()
        {
            TaskCompletionSource completion;
            lock (_gate)
            {
                if (_disposed) return Task.FromException(SinkProvenanceLease.UnavailableError());
                if (_refresh is not null) return _refresh;
                completion = NewSignal();
                _refresh = completion.Task;
            }
            _ = RefreshCore(completion);
            return completion.Task;
        }

        async Task RefreshCore(TaskCompletionSource completion)
        {
            var acquired = false;
            Exception? failure = null;
            try
            {
                await owner._refreshSlots.WaitAsync().ConfigureAwait(false);
                acquired = true;
                var snapshot = SnapshotRoutes(owner._time);
                var state = await ReadRoutes(snapshot).ConfigureAwait(false);
                PublishRefresh(snapshot, state);
            }
            catch (Exception exception)
            {
                failure = exception;
            }
            finally
            {
                if (acquired) owner._refreshSlots.Release();
                lock (_gate) _refresh = null;
            }

            // Drainers may dispose the shared semaphore as soon as this completion is signaled.
            if (failure is null) completion.TrySetResult();
            else completion.TrySetException(failure);
        }

        bool Readable() => !_disposed && _state.IsComplete && !_state.Restricted && Fresh();
        bool Fresh() => _valid && owner._time.GetElapsedTime(_started) < TimeSpan.FromSeconds(2);

        void PublishRevocation()
        {
            lock (_revocationGate)
            {
                if (!_revocationDisposed) _notification ??= NotifyRevocation();
            }
        }

        async Task NotifyRevocation()
        {
            try
            {
                await _revoked.CancelAsync().ConfigureAwait(false);
            }
            catch (AggregateException exception)
            {
                foreach (var failure in exception.Flatten().InnerExceptions) owner._logger.RevocationFailed(failure);
            }
            catch (Exception exception)
            {
                owner._logger.RevocationFailed(exception);
            }
        }

        async Task DisposeRevocation(Task notification)
        {
            await notification.ConfigureAwait(false);
            _revoked.Dispose();
        }

        void Pulse()
        {
            var changed = _changed;
            _changed = NewSignal();
            changed.TrySetResult();
        }
    }
}
