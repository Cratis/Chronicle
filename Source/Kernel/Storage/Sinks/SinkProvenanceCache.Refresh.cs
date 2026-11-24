// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sinks;

public sealed partial class SinkProvenanceCache
{
    internal sealed class Entry(SinkProvenanceCache owner, ISinkProvenanceStorage raw, SinkProvenanceState initial) : IDisposable
    {
        readonly Lock _gate = new();
        readonly CancellationTokenSource _revoked = new();
        TaskCompletionSource _changed = NewSignal();
        SinkProvenanceState _state = initial;
        Task? _refresh;
        long _started;
        bool _valid;
        bool _disposed;
        SinkProvenanceLease? _lease;

        internal SinkProvenanceLease Lease { get { lock (_gate) return _lease ??= new(this); } }
        internal Task InFlight { get { lock (_gate) return _refresh ?? Task.CompletedTask; } }
        internal CancellationToken Revoked => _revoked.Token;
        internal bool IsRestricted { get { lock (_gate) return _state.Restricted; } }

        public void Dispose()
        {
            lock (_gate)
            {
                _disposed = true;
                _valid = false;
                Pulse();
            }
        }

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

        internal void Deliver(Action deliver)
        {
            lock (_gate)
            {
                if (!Readable()) throw SinkProvenanceLease.UnavailableError();
                deliver();
            }
        }

        internal void Deny()
        {
            lock (_gate)
            {
                _state = _state with { Restricted = true };
                Pulse();
            }
            _revoked.Cancel();
        }

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
            if (revoke) _revoked.Cancel();
        }

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
            try { await Refresh(); }
            catch (Exception) { /* Unavailable metadata is not a clean observation. The next poll may recover. */ }
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
        static TaskCompletionSource NewSignal() => new(TaskCreationOptions.RunContinuationsAsynchronously);

        void Pulse()
        {
            var changed = _changed;
            _changed = NewSignal();
            changed.TrySetResult();
        }
    }
}
