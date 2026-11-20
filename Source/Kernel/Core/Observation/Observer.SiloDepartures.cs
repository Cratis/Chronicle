// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.States;
using ConnectedClientsConfiguration = Cratis.Chronicle.Configuration.Clients;
using ISiloDepartures = Cratis.Chronicle.Services.Observation.ISiloDepartures;

namespace Cratis.Chronicle.Observation;

/// <summary>
/// Re-verifies the live subscription of an observer for a while after a silo leaves the cluster.
/// </summary>
/// <remarks>
/// <para>
/// The appended-events queues are grains of their own, and their subscriptions live only in memory. A silo that leaves
/// takes the queues it hosted with it, so an observer on a silo that remains can keep believing it is observing while
/// the queue that replaced its old one, activated afresh on another silo, holds no subscription for it. Nothing reaches
/// such an observer: it is up to date and kept alive, so neither the watchdog's stranded-subscription rescue, which
/// only looks at an observer that is behind, nor a reactivation brings it back before an event goes missing.
/// </para>
/// <para>
/// The signal comes from <see cref="ISiloDepartures"/>, the same one that keeps the subscriptions of connected clients,
/// so it covers every observer - reactors, reducers, projections and webhooks alike. Each departure opens a window of
/// <see cref="ConnectedClientsConfiguration.SubscriptionSettleSeconds"/>, during which a grain timer asks the queue every
/// <see cref="ConnectedClientsConfiguration.ReviseIntervalSeconds"/> whether it still holds the subscription, and re-routes the observer
/// through <see cref="Routing"/> when it does not, the same recovery the watchdog uses. An intact subscription is left
/// alone, and an observer that is not settled in <see cref="Observing"/> - replaying, catching up, failed, quarantined
/// or between states - is not touched: what drives it forward re-subscribes it on its way back into
/// <see cref="Observing"/>.
/// </para>
/// <para>
/// The departure arrives on whichever thread completes it. Awaiting it from the activation resumes on the activation's
/// scheduler, so the window is opened in the grain's context, and the check itself runs as a grain timer turn, which
/// does not interleave with the observer's other calls.
/// </para>
/// </remarks>
public partial class Observer
{
    readonly CancellationTokenSource _siloDeparturesWatch = new();
    IGrainTimer? _departureCheckTimer;
    DateTimeOffset _departureChecksUntil;

    /// <summary>
    /// Gets a value indicating whether checks after a silo departure are scheduled. This property is for testing purposes only.
    /// </summary>
    internal bool HasDepartureChecksScheduled => _departureCheckTimer is not null;

    /// <summary>
    /// Re-verifies the live subscription the way a check after a silo departure does. This method is for testing purposes only.
    /// </summary>
    /// <returns>Awaitable task.</returns>
    internal Task VerifyLiveSubscriptionForTesting() => VerifyLiveSubscription();

    /// <summary>
    /// Opens the window of checks after a silo departure. This method is for testing purposes only.
    /// </summary>
    internal void OpenDepartureChecksForTesting() => OpenDepartureChecks();

    /// <summary>
    /// Runs one tick of the checks after a silo departure. This method is for testing purposes only.
    /// </summary>
    /// <returns>Awaitable task.</returns>
    internal Task RunDepartureCheckForTesting() => CheckAfterDeparture(CancellationToken.None);

    void WatchSiloDepartures()
    {
        if (siloDepartures is null)
        {
            return;
        }

        _ = FollowSiloDepartures(siloDepartures, _siloDeparturesWatch.Token);
    }

    void StopWatchingSiloDepartures()
    {
        if (!_siloDeparturesWatch.IsCancellationRequested)
        {
            _siloDeparturesWatch.Cancel();
        }

        _siloDeparturesWatch.Dispose();
    }

    async Task FollowSiloDepartures(ISiloDepartures departures, CancellationToken cancellationToken)
    {
        // The departure is taken once and replaced only after it has completed, so one that happens while the window
        // is being opened completes the task the next pass waits for instead of a fresh one that never will.
        var departure = departures.Next;
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await departure.WaitAsync(cancellationToken);
                var next = departures.Next;

                // A source that hands back the departure that has already completed is not renewing it; following it
                // would never yield the activation again.
                if (ReferenceEquals(next, departure))
                {
                    return;
                }

                departure = next;
                OpenDepartureChecks();
            }
            catch (OperationCanceledException)
            {
                return;
            }
            catch (Exception ex)
            {
                logger.OpeningDepartureChecksFailed(ex);
                if (departure.IsCompleted && ReferenceEquals(departures.Next, departure))
                {
                    return;
                }

                departure = departures.Next;
            }
        }
    }

    void OpenDepartureChecks()
    {
        var clients = optionsMonitor.CurrentValue?.ConnectedClients ?? new ConnectedClientsConfiguration();
        var interval = TimeSpan.FromSeconds(Math.Max(clients.ReviseIntervalSeconds, 1));
        _departureChecksUntil = _clock.GetUtcNow() + TimeSpan.FromSeconds(Math.Max(clients.SubscriptionSettleSeconds, 0));

        // Every observer hears the same departure, so the first check is spread over one interval rather than having
        // all of them ask their queues in the same moment.
        _departureCheckTimer ??= this.RegisterGrainTimer(
            CheckAfterDeparture,
            new GrainTimerCreationOptions
            {
                DueTime = TimeSpan.FromMilliseconds(Random.Shared.NextDouble() * interval.TotalMilliseconds),
                Period = interval
            });
    }

    async Task CheckAfterDeparture(CancellationToken cancellationToken)
    {
        if (_clock.GetUtcNow() > _departureChecksUntil)
        {
            _departureCheckTimer?.Dispose();
            _departureCheckTimer = null;
            return;
        }

        try
        {
            await VerifyLiveSubscription();
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // The queue can be unreachable while the cluster is still settling; the next tick asks again.
            logger.VerifyingLiveSubscriptionFailed(ex);
        }
    }

    async Task VerifyLiveSubscription()
    {
        using var scope = logger.BeginObserverScope(_observerId, _observerKey);
        if (!IsSettledLiveObserver() || !await IsObservingWithNoTransitionInProgress())
        {
            return;
        }

        if (!await QueueLostSubscription())
        {
            return;
        }

        logger.RestoringLiveSubscriptionAfterSiloDeparture(State.NextEventSequenceNumber);
        await TransitionTo<Routing>();
    }

    bool IsSettledLiveObserver() =>
        IsObserverLive() &&
        KeyOwnsDefinition &&
        _subscription.IsSubscribed &&
        State.RunningState == ObserverRunningState.Active;
}
