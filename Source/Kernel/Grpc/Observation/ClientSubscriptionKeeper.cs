// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Clients;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Services.Observation;

/// <summary>
/// Keeps the subscription of a connected client in place for as long as its observation stream is open.
/// </summary>
/// <remarks>
/// <para>
/// A subscription lives in grain activations only - the observer's and the client registration's. Either can be lost
/// while the client stays connected: with a silo that leaves the cluster, or when Orleans deactivates activations on a
/// surviving silo because the silo owning their directory entries left. The client never registers again in that case,
/// so the open stream is the only thing that outlives both activations and still knows the client is there.
/// </para>
/// <para>
/// Checking costs grain calls per subscription, so how often is decided by <see cref="SubscriptionCheckSchedule"/>: often
/// while a subscription is not confirmed intact and for a while after a silo starts leaving the cluster, which is what
/// loses subscriptions, and only at a slow, jittered safety-net interval otherwise. The checks after a departure are
/// spread over one revise interval, so the keepers of every stream on a silo do not all call their observers at once.
/// </para>
/// <para>
/// A failed check is logged and retried at the next one; a silo that is still leaving the cluster can fail a call or two.
/// </para>
/// </remarks>
internal static class ClientSubscriptionKeeper
{
    /// <summary>
    /// Keep the subscription until the stream closes.
    /// </summary>
    /// <param name="keep">Keeps the subscription in place, restoring it if it was lost. Returns whether it was found intact.</param>
    /// <param name="schedule">The <see cref="SubscriptionCheckSchedule"/> deciding when to check.</param>
    /// <param name="siloDepartures">The <see cref="ISiloDepartures"/> telling when a silo leaves the cluster.</param>
    /// <param name="timeProvider">The <see cref="TimeProvider"/> to wait with.</param>
    /// <param name="connectionId">The <see cref="ConnectionId"/> of the client.</param>
    /// <param name="logger">The <see cref="ILogger"/> to log with.</param>
    /// <param name="cancellationToken">Cancelled when the stream closes.</param>
    /// <returns>A task that completes when the stream has closed.</returns>
    public static async Task KeepWhileConnected(
        Func<Task<bool>> keep,
        SubscriptionCheckSchedule schedule,
        ISiloDepartures siloDepartures,
        TimeProvider timeProvider,
        ConnectionId connectionId,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        try
        {
            // The departure is taken once and replaced only after it has been acted on: a silo that leaves while a check
            // or the spread runs completes this task, and the next pass sees it instead of a fresh one that never will.
            var departure = siloDepartures.Next;
            while (true)
            {
                if (departure.IsCompleted || await WaitForCheck(schedule.NextCheckAfter(timeProvider.GetUtcNow()), departure, timeProvider, cancellationToken))
                {
                    departure = siloDepartures.Next;
                    schedule.SiloLeft(timeProvider.GetUtcNow());
                    await Task.Delay(schedule.SpreadAfterDeparture(), timeProvider, cancellationToken);
                }

                schedule.Checked(await Keep(keep, connectionId, logger, cancellationToken));
            }
        }
        catch (OperationCanceledException)
        {
            // The stream closed; there is nothing left to keep.
        }
    }

    /// <summary>
    /// Keeps one subscription, sending its definition only when the subscription is not intact.
    /// </summary>
    /// <param name="isIntact">Asks the observer's client grain whether there is nothing to restore - a call that carries no definition.</param>
    /// <param name="keep">Sends the definition so the grain can restore what was lost.</param>
    /// <returns>True when the subscription was intact; false when the definition was sent to restore it.</returns>
    /// <remarks>
    /// A failing check - a silo of an older version during a rolling deployment does not know it - is treated as not
    /// intact, so the subscription is kept the way it always was rather than not at all.
    /// </remarks>
    public static async Task<bool> KeepSubscription(Func<Task<bool>> isIntact, Func<Task> keep)
    {
        var intact = false;
        try
        {
            intact = await isIntact();
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Falls through to the full keep below.
        }

        if (!intact)
        {
            await keep();
        }

        return intact;
    }

    /// <summary>
    /// Confirms that a subscription is intact without asking the observer when the observer is dormant and holds this client.
    /// </summary>
    /// <param name="isIntact">The check that asks the observer's client grain, used whenever the observer has to be asked.</param>
    /// <param name="dormantObservers">The <see cref="IDormantObservers"/> that tells whether the observer is dormant and holds the client; none means every check asks.</param>
    /// <param name="key">The <see cref="ConnectedObserverKey"/> of the observer served by the stream.</param>
    /// <returns>A check that is true when the observer is dormant and its record names this client on this silo.</returns>
    /// <remarks>
    /// <para>
    /// Asking the observer is what wakes it, and a dormant observer is subscribed by its record and its queue entry, so
    /// there is nothing to restore by asking. An observer that is resident, has no record, has a record naming another
    /// connection or silo, a read that fails, the legacy keeper mode, a setting that does not apply or a mixed cluster all
    /// fall back to the call, which restores whatever was lost - including the live subscription of a resident observer whose
    /// silo was lost, which a record alone would hide.
    /// </para>
    /// </remarks>
    public static Func<Task<bool>> ConfirmFromDurableSubscription(Func<Task<bool>> isIntact, IDormantObservers? dormantObservers, ConnectedObserverKey key) =>
        async () =>
        {
            if (dormantObservers is not null)
            {
                try
                {
                    if (await dormantObservers.HoldsClient(new ObserverKey(key.ObserverId, key.EventStore, key.Namespace, key.EventSequenceId), key.ConnectionId))
                    {
                        return true;
                    }
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    // Falls through to the call below.
                }
            }

            return await isIntact();
        };

    /// <summary>
    /// Confirms that a subscription is intact by asking the observer itself, without calling the client grain of the stream.
    /// </summary>
    /// <param name="isIntact">The check that asks the observer's client grain, used whenever the observer does not know the client.</param>
    /// <param name="observer">The <see cref="IObserver"/> served by the stream.</param>
    /// <param name="connectionId">The <see cref="ConnectionId"/> of the client the stream belongs to.</param>
    /// <returns>A check that is true when the observer knows the client, as a subscription target or as a superseded registration.</returns>
    /// <remarks>
    /// <para>
    /// A client grain exists per stream, and every call to it keeps its activation resident. A keeper that goes through it on every
    /// check therefore keeps one activation per stream alive for as long as the client is connected, which for a service with several
    /// replicas is several activations per observer. Whether the observer holds the client is the observer's own knowledge: when it
    /// does, there is nothing to restore, so the client grain is not called and goes idle until it is needed. An observer that does
    /// not know the client, a webhook-served observer, a failing call or an older silo all fall back to the call, which restores
    /// whatever was lost.
    /// </para>
    /// <para>
    /// A superseded registration counts as known. It receives nothing until it is promoted; when a promotion fails, the registration
    /// is no longer known and the next check restores it, so it is found at the safety-net interval and no longer by a check every
    /// revise interval for as long as the client stays connected.
    /// </para>
    /// </remarks>
    public static Func<Task<bool>> ConfirmFromObserver(Func<Task<bool>> isIntact, IObserver observer, ConnectionId connectionId) =>
        async () =>
        {
            try
            {
                if (await observer.KnowsClient(connectionId))
                {
                    return true;
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // Falls through to the call below.
            }

            return await isIntact();
        };

    static async Task<bool> WaitForCheck(TimeSpan interval, Task departure, TimeProvider timeProvider, CancellationToken cancellationToken)
    {
        using var waiting = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var elapsed = Task.Delay(interval, timeProvider, waiting.Token);
        var first = await Task.WhenAny(elapsed, departure);
        await waiting.CancelAsync();
        cancellationToken.ThrowIfCancellationRequested();
        return first == departure;
    }

    static async Task<bool> Keep(Func<Task<bool>> keep, ConnectionId connectionId, ILogger logger, CancellationToken cancellationToken)
    {
        try
        {
            return await keep();
        }
        catch (Exception ex)
        {
            // A stream that closed while checking needs nothing; its disconnect unsubscribes the client.
            if (!cancellationToken.IsCancellationRequested)
            {
                logger.KeepingClientSubscriptionFailed(connectionId, ex);
            }

            return false;
        }
    }
}
