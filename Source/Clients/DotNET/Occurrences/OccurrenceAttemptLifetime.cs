// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Occurrences;

/// <summary>
/// Owns one run's local abandonment budget, not proof of server ownership.
/// </summary>
internal sealed class OccurrenceAttemptLifetime : IAsyncDisposable
{
    readonly object _gate = new();
    readonly TimeProvider _clock;
    readonly CancellationToken _session;
    readonly CancellationTokenSource _lifetime = new();
    readonly TaskCompletionSource _cancelled = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly CancellationTokenRegistration _registration;
    readonly Task _watchdog;
    long _anchor;
    bool _abandoned;
    bool _expired;
    bool _settled;

    /// <summary>
    /// Initializes one run's local abandonment budget.
    /// </summary>
    /// <param name="clock">The monotonic clock.</param>
    /// <param name="anchor">The timestamp at claim admission.</param>
    /// <param name="session">The owning connection session's cancellation.</param>
    internal OccurrenceAttemptLifetime(TimeProvider clock, long anchor, CancellationToken session)
    {
        _clock = clock;
        _anchor = anchor;
        _session = session;
        _registration = session.UnsafeRegister(_ => Abandon(), null);
        _watchdog = Watch();
    }

    /// <summary>
    /// Gets the cancellation token for this run's admitted work.
    /// </summary>
    internal CancellationToken Token => _lifetime.Token;

    /// <summary>
    /// Gets a value indicating whether the budget ran out without a confirmed renewal.
    /// </summary>
    internal bool Expired
    {
        get
        {
            lock (_gate) return _expired;
        }
    }

    /// <summary>
    /// Cancels admitted work and releases this run's resources.
    /// </summary>
    /// <returns>The asynchronous disposal.</returns>
    public async ValueTask DisposeAsync()
    {
        Abandon();
        await _registration.DisposeAsync();
        await _watchdog;
        await _cancelled.Task;
        _lifetime.Dispose();
    }

    /// <summary>
    /// Stops waiting on cancellation while observing any eventual operation fault.
    /// </summary>
    /// <typeparam name="T">The operation result type.</typeparam>
    /// <param name="operation">The already-started operation.</param>
    /// <param name="cancellationToken">The local cancellation.</param>
    /// <returns>The operation result if the local wait completes.</returns>
    internal static async Task<T> Wait<T>(Task<T> operation, CancellationToken cancellationToken)
    {
        try { return await operation.WaitAsync(cancellationToken); }
        finally { Observe(operation); }
    }

    /// <summary>
    /// Admits one operation while the local budget and session remain active.
    /// </summary>
    /// <param name="settlement">Whether to reserve this run's only settlement.</param>
    /// <returns>A single-use ticket, or null when admission is refused.</returns>
    internal Operation? Admit(bool settlement = false)
    {
        lock (_gate)
        {
            var now = _clock.GetTimestamp();
            if (Active(now) && (!settlement || !_settled))
            {
                if (settlement) _settled = true;
                return new(now, Token);
            }
        }
        Abandon();
        return null;
    }

    /// <summary>
    /// Refreshes the budget at admission time only for a timely acknowledgment.
    /// </summary>
    /// <param name="operation">The acknowledged renewal's admission.</param>
    /// <returns>Whether the acknowledgment was accepted.</returns>
    internal bool Accept(Operation operation)
    {
        lock (_gate)
        {
            if (Active(_clock.GetTimestamp()))
            {
                _anchor = operation.Timestamp;
                return true;
            }
        }
        Abandon();
        return false;
    }

    /// <summary>
    /// Checks that a full retry backoff fits strictly within the active budget.
    /// </summary>
    /// <returns>Whether a retry delay may begin.</returns>
    internal bool CanRetry()
    {
        lock (_gate)
        {
            var now = _clock.GetTimestamp();
            if (Active(now) && Remaining(now) > TimeSpan.FromSeconds(1)) return true;
        }
        Abandon();
        return false;
    }

    /// <summary>
    /// Irreversibly closes admission and cancels work outside the state gate.
    /// </summary>
    internal void Abandon()
    {
        lock (_gate)
        {
            if (_abandoned) return;
            _abandoned = true;
        }
        Observe(Cancel());
    }

    static void Observe(Task task) => _ = task.ContinueWith(
        completed => _ = completed.Exception,
        CancellationToken.None,
        TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously,
        TaskScheduler.Default);

    bool Active(long now) => !_abandoned && !_session.IsCancellationRequested && Remaining(now) > TimeSpan.Zero;

    TimeSpan Remaining(long now) => TimeSpan.FromSeconds(OccurrenceAttempt.LeaseSeconds) - _clock.GetElapsedTime(_anchor, now);

    async Task Cancel()
    {
        try { await _lifetime.CancelAsync(); }
        finally { _cancelled.TrySetResult(); }
    }

    async Task Watch()
    {
        try
        {
            while (true)
            {
                TimeSpan remaining;
                lock (_gate)
                {
                    var now = _clock.GetTimestamp();
                    remaining = Active(now) ? Remaining(now) : TimeSpan.Zero;
                    _expired = remaining <= TimeSpan.Zero && !_abandoned && !_session.IsCancellationRequested;
                }
                if (remaining <= TimeSpan.Zero)
                {
                    Abandon();
                    return;
                }

                // An earlier deadline is safe: recheck the current anchor after every queued callback.
                await Task.Delay(remaining, _clock, Token);
            }
        }
        catch (OperationCanceledException) when (Token.IsCancellationRequested)
        {
            // Abandonment also disposes the pending clock delay.
        }
    }

    /// <summary>
    /// Single-use admission; invocation is outside the gate and may follow abandonment.
    /// </summary>
    /// <param name="timestamp">The monotonic timestamp at admission.</param>
    /// <param name="token">The run's cancellation token.</param>
    internal sealed class Operation(long timestamp, CancellationToken token)
    {
        int _used;

        /// <summary>
        /// Gets the monotonic timestamp at admission.
        /// </summary>
        internal long Timestamp => timestamp;

        /// <summary>
        /// Invokes admitted work once outside the state gate.
        /// </summary>
        /// <typeparam name="T">The operation result type.</typeparam>
        /// <param name="invoke">The external operation to invoke.</param>
        /// <returns>The cancellation-aware local wait for the operation.</returns>
        /// <exception cref="InvalidOperationException">The admission has already been used.</exception>
        internal Task<T> Invoke<T>(Func<CancellationToken, Task<T>> invoke)
        {
            if (Interlocked.Exchange(ref _used, 1) != 0) throw new InvalidOperationException("An occurrence operation admission can only be used once.");
            return Wait(invoke(token), token);
        }
    }
}
