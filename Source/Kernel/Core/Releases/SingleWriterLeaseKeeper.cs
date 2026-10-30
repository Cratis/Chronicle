// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Clustering;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Storage.SingleWriter;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans.Configuration;

namespace Cratis.Chronicle.SingleWriter;

/// <summary>
/// Fences Localhost clustering before runtime initialization and until all silo work has stopped.
/// </summary>
public class SingleWriterLeaseKeeper : ISingleWriterLease, ILifecycleParticipant<ISiloLifecycle>, IDisposable
{
    readonly object _gate = new();
    readonly ISingleWriterLeaseStorage _storage;
    readonly SingleWriterLease _options;
    readonly SingleWriterLeaseClaim _claim;
    readonly IHostApplicationLifetime _lifetime;
    readonly IShutdownDrainBudget _drain;
    readonly TimeProvider _time;
    readonly ISingleWriterLeaseFailFast _failFast;
    readonly ILogger<SingleWriterLeaseKeeper> _logger;
    readonly CancellationTokenSource _renewalStop = new();
    readonly ITimer _stopDeadline;
    readonly ITimer _watchdog;
    readonly TimeSpan _retryInterval;
    Task _renewal = Task.CompletedTask;
    long _lastSuccessStart;
    bool _acquired;
    volatile bool _stopped;
    volatile bool _held;

    /// <summary>
    /// Initializes a new instance of the <see cref="SingleWriterLeaseKeeper"/> class.
    /// </summary>
    /// <param name="storage">The atomic lease storage.</param>
    /// <param name="options">The lease configuration.</param>
    /// <param name="clusterOptions">The diagnostic cluster identity.</param>
    /// <param name="lifetime">The host lifetime.</param>
    /// <param name="drain">The shared shutdown drain budget.</param>
    /// <param name="timeProvider">The monotonic clock and timer provider.</param>
    /// <param name="failFast">The hard-fence implementation.</param>
    /// <param name="hostOptions">The host shutdown timeout.</param>
    /// <param name="logger">The logger.</param>
    public SingleWriterLeaseKeeper(
        ISingleWriterLeaseStorage storage,
        IOptions<ChronicleOptions> options,
        IOptions<ClusterOptions> clusterOptions,
        IHostApplicationLifetime lifetime,
        IShutdownDrainBudget drain,
        TimeProvider timeProvider,
        ISingleWriterLeaseFailFast failFast,
        IOptions<HostOptions> hostOptions,
        ILogger<SingleWriterLeaseKeeper> logger)
    {
        _storage = storage;
        _options = options.Value.Clustering.SingleWriterLease;
        _lifetime = lifetime;
        _drain = drain;
        _time = timeProvider;
        _failFast = failFast;
        _logger = logger;
        _claim = new(Owner, _options.Ttl, clusterOptions.Value.ClusterId, clusterOptions.Value.ServiceId, Environment.MachineName, typeof(SingleWriterLeaseKeeper).Assembly.GetName().Version?.ToString() ?? "unknown");
        _retryInterval = TimeSpan.FromTicks(Math.Min(TimeSpan.FromSeconds(2).Ticks, _options.Ttl.Ticks / 10));
        _stopDeadline = _time.CreateTimer(_ => CheckDeadline(), null, Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
        _watchdog = _time.CreateTimer(_ => HardFence(), null, Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
        if (_options.Ttl / 2 <= hostOptions.Value.ShutdownTimeout)
        {
            _logger.TtlShorterThanShutdown(_options.Ttl / 2, hostOptions.Value.ShutdownTimeout);
        }
    }

    /// <inheritdoc/>
    public bool IsHeld => _held;

    /// <inheritdoc/>
    public SingleWriterLeaseOwner Owner { get; } = SingleWriterLeaseOwner.New();

    /// <inheritdoc/>
    public void Participate(ISiloLifecycle lifecycle) => lifecycle.Subscribe(
        nameof(SingleWriterLeaseKeeper), ServiceLifecycleStage.RuntimeInitialize - 1, OnStart, OnStop);

    /// <inheritdoc/>
    public void Dispose()
    {
        _renewalStop.Cancel();
        _stopDeadline.Dispose();
        _watchdog.Dispose();
        _renewalStop.Dispose();
    }

    async Task OnStart(CancellationToken cancellationToken)
    {
        _logger.Acquiring(Owner.Value);
        var started = _time.GetTimestamp();
        var lastWaitingLog = TimeSpan.FromSeconds(-10);
        SingleWriterLeaseHolder? holder = null;
        using var timeout = new CancellationTokenSource(_options.AcquireTimeout, _time);
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeout.Token);
        do
        {
            cancellationToken.ThrowIfCancellationRequested();
            var attemptStarted = _time.GetTimestamp();
            try
            {
                // A zero timeout still makes one immediate attempt.
                var token = _options.AcquireTimeout == TimeSpan.Zero ? cancellationToken : linked.Token;
                var attempt = await _storage.TryAcquire(_claim, token).WaitAsync(token);
                token.ThrowIfCancellationRequested();
                holder = attempt.Holder;
                if (attempt.Acquired)
                {
                    lock (_gate)
                    {
                        _acquired = true;
                        _held = true;
                        RecordSuccess(attemptStarted);
                        if (Remaining(_options.Ttl / 2) == TimeSpan.Zero)
                        {
                            LoseLease(false);
                            throw new SingleWriterLeaseNotAcquired(holder);
                        }
                    }

                    _logger.Acquired(_time.GetElapsedTime(started));
                    _renewal = RenewLoop();
                    return;
                }
            }
            catch (OperationCanceledException) when (timeout.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception) when (exception is not SingleWriterLeaseNotAcquired && !cancellationToken.IsCancellationRequested)
            {
                _logger.RenewalFailed(exception);
            }

            var waited = _time.GetElapsedTime(started);
            if (waited - lastWaitingLog >= TimeSpan.FromSeconds(10))
            {
                _logger.WaitingForLease(holder?.Owner.Value, holder?.HostName, holder?.Version, holder?.ExpiresAt);
                lastWaitingLog = waited;
            }

            var remaining = _options.AcquireTimeout - waited;
            if (remaining <= TimeSpan.Zero) break;
            await Task.Delay(remaining < _retryInterval ? remaining : _retryInterval, _time, cancellationToken);
        }
        while (_time.GetElapsedTime(started) < _options.AcquireTimeout);

        cancellationToken.ThrowIfCancellationRequested();
        _logger.NotAcquired();
        throw new SingleWriterLeaseNotAcquired(holder);
    }

    async Task RenewLoop()
    {
        var delay = _options.Ttl / 3;
        try
        {
            while (!_renewalStop.IsCancellationRequested)
            {
                await Task.Delay(delay, _time, _renewalStop.Token);
                var started = _time.GetTimestamp();
                try
                {
                    var renewed = await _storage.Renew(Owner, _options.Ttl, _renewalStop.Token).WaitAsync(_renewalStop.Token);
                    lock (_gate)
                    {
                        if (!_held || _stopped) return;
                        if (!renewed)
                        {
                            LoseLease(true);
                            return;
                        }

                        if (Remaining(_options.Ttl / 2) == TimeSpan.Zero)
                        {
                            LoseLease(false);
                            return;
                        }

                        RecordSuccess(started);
                    }

                    delay = _options.Ttl / 3;
                }
                catch (Exception exception) when (!_renewalStop.IsCancellationRequested)
                {
                    _logger.RenewalFailed(exception);
                    delay = _retryInterval;
                }
            }
        }
        catch (OperationCanceledException) when (_renewalStop.IsCancellationRequested)
        {
            // Lease loss or silo shutdown ends renewal; the independent watchdog remains armed on loss.
        }
    }

    void RecordSuccess(long started)
    {
        _lastSuccessStart = started;
        _stopDeadline.Change(Remaining(_options.Ttl / 2), Timeout.InfiniteTimeSpan);
    }

    TimeSpan Remaining(TimeSpan deadline) => TimeSpan.FromTicks(Math.Max(0, (deadline - _time.GetElapsedTime(_lastSuccessStart)).Ticks));

    void CheckDeadline()
    {
        lock (_gate)
        {
            if (!_held || _stopped) return;
            var remaining = Remaining(_options.Ttl / 2);
            if (remaining > TimeSpan.Zero)
            {
                _stopDeadline.Change(remaining, Timeout.InfiniteTimeSpan);
                return;
            }

            LoseLease(false);
        }
    }

    void LoseLease(bool anotherOwner)
    {
        _held = false;
        _logger.LeaseLost(Owner.Value);
        Environment.ExitCode = 1;

        // Arm independently of the renewal task and host callbacks, either of which can be stuck.
        _watchdog.Change(anotherOwner ? TimeSpan.Zero : Remaining(_options.Ttl - TimeSpan.FromSeconds(1)), Timeout.InfiniteTimeSpan);
        _renewalStop.Cancel();
        _drain.Begin();
        _lifetime.StopApplication();
    }

    void HardFence()
    {
        // Never take the state lock here: a host shutdown callback can hang while holding it.
        if (!_stopped) _failFast.FailFast($"Single-writer lease expired for {Owner.Value}; the process did not stop safely.");
    }

    async Task OnStop(CancellationToken cancellationToken)
    {
        if (_stopped) return;
        using var releaseTimeout = new CancellationTokenSource(TimeSpan.FromSeconds(3));
        try
        {
            await _renewalStop.CancelAsync().WaitAsync(releaseTimeout.Token);
            await _renewal.WaitAsync(releaseTimeout.Token);
            if (_acquired)
            {
                await _storage.Release(Owner, releaseTimeout.Token).WaitAsync(releaseTimeout.Token);
                _logger.Released();
            }
        }
        catch (Exception exception)
        {
            _logger.ReleaseFailed(exception);
        }
        finally
        {
            // This lifecycle stage stops after grains and runtime services, so no writer remains.
            lock (_gate)
            {
                _stopped = true;
                _held = false;
                _stopDeadline.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
                _watchdog.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
            }
        }
    }
}
