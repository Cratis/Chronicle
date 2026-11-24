// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Diagnostics.OpenTelemetry.Tracing;
using Cratis.Chronicle.Services.Observation;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ConnectedClientsConfiguration = Cratis.Chronicle.Configuration.Clients;

namespace Cratis.Chronicle.Supervision;

/// <summary>
/// Runs the watchdog and the checks after a silo departure for every observer resident on this silo, with one loop.
/// </summary>
/// <remarks>
/// <para>
/// Every resident grain with a duty registers on activation and unregisters on deactivation. A tick of
/// <see cref="TickEvery"/> finds the registrations whose check is due and runs each one, through the grain, so it does
/// not interleave with the grain's other calls, exactly as the grain timer it replaces. A check that is still running is
/// skipped, and the next is due an interval after the last finished, so a slow grain never piles up turns.
/// </para>
/// <para>
/// An activation that is alive no longer notices anything by itself. The supervisor makes up for it with the signal that
/// matters most: a silo leaving the cluster, which <see cref="ISiloDepartures"/> reports. It opens a window of
/// <see cref="ConnectedClientsConfiguration.SubscriptionSettleSeconds"/>, and in that window the supervisor runs the
/// after-departure duty of every registration once per <see cref="ConnectedClientsConfiguration.ReviseIntervalSeconds"/>.
/// Each pass is spread over its interval in batches, so the grains do not ask their neighbours in the same moment.
/// </para>
/// <para>
/// <see cref="LastTick"/> tells when the loop last ran. A supervisor that stops ticking leaves grains unwatched, so
/// a stalled loop is visible there.
/// </para>
/// </remarks>
/// <param name="options">The <see cref="IOptionsMonitor{TOptions}"/> for the <see cref="ChronicleOptions"/>.</param>
/// <param name="logger">The <see cref="ILogger"/> for logging.</param>
/// <param name="siloDepartures">The optional <see cref="ISiloDepartures"/> telling when a silo leaves the cluster.</param>
/// <param name="timeProvider">The optional <see cref="TimeProvider"/>.</param>
public sealed class SiloSupervisor(
    IOptionsMonitor<ChronicleOptions> options,
    ILogger<SiloSupervisor> logger,
    ISiloDepartures? siloDepartures = null,
    TimeProvider? timeProvider = null) : ISiloSupervisor, ILifecycleParticipant<ISiloLifecycle>, IDisposable
{
    /// <summary>
    /// How many registrations are asked to run their duty at once during a departure pass.
    /// </summary>
    internal const int DepartureBatchSize = 32;

    /// <summary>
    /// How often the registry is looked at for observers whose watchdog is due.
    /// </summary>
    internal static readonly TimeSpan TickEvery = TimeSpan.FromSeconds(1);

    /// <summary>
    /// The shortest wait between two checks of the same registration; a shorter one would make the supervisor a tight loop of grain calls.
    /// </summary>
    internal static readonly TimeSpan MinimumInterval = TimeSpan.FromSeconds(1);

    /// <summary>
    /// How long a stop waits for the loops to return.
    /// </summary>
    internal static readonly TimeSpan StopWait = TimeSpan.FromSeconds(1);

    readonly TimeProvider _time = timeProvider ?? TimeProvider.System;
    readonly ConcurrentDictionary<string, Registration> _registrations = new();
    readonly ISiloDepartures _departures = siloDepartures ?? NoSiloDepartures.Instance;
    CancellationTokenSource? _cancellation;
    Task _tickLoop = Task.CompletedTask;
    Task _departureLoop = Task.CompletedTask;
    Task _departurePass = Task.CompletedTask;
    DateTimeOffset _departureWindowUntil;
    DateTimeOffset _nextDeparturePass;

    /// <summary>
    /// Gets the number of observers registered.
    /// </summary>
    public int Count => _registrations.Count;

    /// <summary>
    /// Gets when the loop last looked at the registry.
    /// </summary>
    public DateTimeOffset LastTick { get; private set; }

    /// <summary>
    /// Gets a value indicating whether the window after a silo departure is open.
    /// </summary>
    public bool IsDepartureWindowOpen => _time.GetUtcNow() <= _departureWindowUntil;

    /// <inheritdoc/>
    public void Register(string key, TimeSpan? interval, Func<Task<TimeSpan?>>? check, Func<Task>? afterDeparture)
    {
        var effectiveInterval = interval;
        if (interval is { } given && given < MinimumInterval)
        {
            effectiveInterval = MinimumInterval;
        }

        var due = effectiveInterval is { } period ? _time.GetUtcNow() + period : DateTimeOffset.MaxValue;
        _registrations[key] = new Registration(check, afterDeparture, effectiveInterval, due);
    }

    /// <inheritdoc/>
    public void Unregister(string key) => _registrations.TryRemove(key, out _);

    /// <inheritdoc/>
    public void Participate(ISiloLifecycle lifecycle) =>
        lifecycle.Subscribe(nameof(SiloSupervisor), ServiceLifecycleStage.Active, Start, Stop);

    /// <inheritdoc/>
    public void Dispose() => _cancellation?.Dispose();

    /// <summary>
    /// Opens the window after a silo departure.
    /// </summary>
    internal void OpenDepartureWindow()
    {
        var clients = options.CurrentValue?.ConnectedClients ?? new ConnectedClientsConfiguration();
        var now = _time.GetUtcNow();
        _departureWindowUntil = now + TimeSpan.FromSeconds(Math.Max(clients.SubscriptionSettleSeconds, 0));
        if (_nextDeparturePass < now)
        {
            _nextDeparturePass = now;
        }
    }

    /// <summary>
    /// Starts the watchdog of every observer that is due and returns when they have all finished.
    /// </summary>
    /// <returns>Awaitable task.</returns>
    internal Task RunDueWatchdogs() => Task.WhenAll(StartDueWatchdogs());

    /// <summary>
    /// Runs one pass of the checks after a silo departure, asking every resident observer to verify its subscription.
    /// </summary>
    /// <param name="spread">How long to spread the batches over.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> that stops the pass.</param>
    /// <returns>Awaitable task.</returns>
    internal async Task RunDeparturePass(TimeSpan spread, CancellationToken cancellationToken = default)
    {
        var observers = _registrations.Where(entry => entry.Value.AfterDeparture is not null).ToArray();
        var batches = observers.Chunk(DepartureBatchSize).ToArray();
        var pause = batches.Length > 1 ? spread / batches.Length : TimeSpan.Zero;
        for (var index = 0; index < batches.Length; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await Task.WhenAll(batches[index].Select(entry => VerifyAfterDeparture(entry.Key, entry.Value)));
            if (pause > TimeSpan.Zero && index < batches.Length - 1)
            {
                await Task.Delay(pause, _time, cancellationToken);
            }
        }
    }

    /// <summary>
    /// Looks at the registry once: starts the watchdogs that are due and, in the window after a departure, the next pass.
    /// </summary>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> that stops a pass of the checks after a departure.</param>
    /// <returns>The tasks of the watchdogs started.</returns>
    internal Task[] Tick(CancellationToken cancellationToken)
    {
        LastTick = _time.GetUtcNow();
        StartDeparturePassIfDue(cancellationToken);
        return StartDueWatchdogs();
    }

    Task[] StartDueWatchdogs()
    {
        var now = _time.GetUtcNow();
        List<Task>? started = null;
        foreach (var (key, registration) in _registrations)
        {
            if (registration.Check is null || registration.IsRunning || registration.Due > now)
            {
                continue;
            }

            registration.IsRunning = true;
            (started ??= []).Add(DetachedExecution.Run(() => Check(key, registration)));
        }

        return started?.ToArray() ?? [];
    }

    async Task Check(string key, Registration registration)
    {
        TimeSpan? next = null;
        try
        {
            next = await registration.Check!();
        }
        catch (Exception ex)
        {
            logger.CheckingFailed(key, ex);
        }
        finally
        {
            var wait = next ?? registration.Interval ?? TimeSpan.FromDays(1);
            registration.Due = _time.GetUtcNow() + (wait < MinimumInterval ? MinimumInterval : wait);
            registration.IsRunning = false;
        }
    }

    async Task VerifyAfterDeparture(string key, Registration registration)
    {
        try
        {
            await registration.AfterDeparture!();
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // The queue can be unreachable while the cluster is still settling; the next pass asks again.
            logger.CheckingAfterDepartureFailed(key, ex);
        }
    }

    void StartDeparturePassIfDue(CancellationToken cancellationToken)
    {
        var now = _time.GetUtcNow();
        if (now > _departureWindowUntil || now < _nextDeparturePass || !_departurePass.IsCompleted)
        {
            return;
        }

        var clients = options.CurrentValue?.ConnectedClients ?? new ConnectedClientsConfiguration();
        var interval = TimeSpan.FromSeconds(Math.Max(clients.ReviseIntervalSeconds, 1));
        _nextDeparturePass = now + interval;
        _departurePass = DetachedExecution.Run(async () =>
        {
            try
            {
                await RunDeparturePass(interval, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                // Stopping.
            }
            catch (Exception ex)
            {
                logger.DeparturePassFailed(ex);
            }
        });
    }

    Task Start(CancellationToken cancellationToken)
    {
        _cancellation = new();
        var token = _cancellation.Token;
        LastTick = _time.GetUtcNow();
        _tickLoop = DetachedExecution.Run(() => TickLoop(token));
        _departureLoop = DetachedExecution.Run(() => FollowSiloDepartures(token));
        return Task.CompletedTask;
    }

    async Task Stop(CancellationToken cancellationToken)
    {
        if (_cancellation is null)
        {
            return;
        }

        await _cancellation.CancelAsync();
        try
        {
            await Task.WhenAll(_tickLoop, _departureLoop, _departurePass).WaitAsync(StopWait, _time, cancellationToken);
        }
        catch (Exception ex) when (ex is OperationCanceledException or TimeoutException)
        {
            // Stopping.
        }
    }

    async Task TickLoop(CancellationToken cancellationToken)
    {
        using var timer = new PeriodicTimer(TickEvery, _time);
        try
        {
            while (await timer.WaitForNextTickAsync(cancellationToken))
            {
                try
                {
                    Tick(cancellationToken);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    logger.TickFailed(ex);
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Stopping.
        }
    }

    async Task FollowSiloDepartures(CancellationToken cancellationToken)
    {
        // The departure is taken once and replaced only after it has completed, so one that happens while the window
        // is being opened completes the task the next pass waits for instead of a fresh one that never will.
        var departure = _departures.Next;
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await departure.WaitAsync(cancellationToken);
                var next = _departures.Next;

                // A source that hands back the departure that has already completed is not renewing it; following it
                // would never yield again.
                if (ReferenceEquals(next, departure))
                {
                    return;
                }

                departure = next;
                OpenDepartureWindow();
            }
            catch (OperationCanceledException)
            {
                return;
            }
            catch (Exception ex)
            {
                logger.OpeningDepartureWindowFailed(ex);
                if (departure.IsCompleted && ReferenceEquals(_departures.Next, departure))
                {
                    return;
                }

                departure = _departures.Next;
            }
        }
    }

    sealed class Registration(Func<Task<TimeSpan?>>? check, Func<Task>? afterDeparture, TimeSpan? interval, DateTimeOffset due)
    {
        public volatile bool IsRunning;

        public Func<Task<TimeSpan?>>? Check { get; } = check;

        public Func<Task>? AfterDeparture { get; } = afterDeparture;

        public TimeSpan? Interval { get; } = interval;

        public DateTimeOffset Due { get; set; } = due;
    }
}
