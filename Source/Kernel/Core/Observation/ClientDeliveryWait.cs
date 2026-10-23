// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Observation;

/// <summary>
/// Bounds the subscriber turn, independently of the caller abandoning its wait.
/// </summary>
/// <param name="clock">The clock.</param>
/// <param name="logger">The logger.</param>
/// <param name="observer">The observer.</param>
/// <param name="partition">The partition.</param>
internal class ClientDeliveryWait(TimeProvider clock, ILogger logger, ObserverKey observer, Key partition)
{
    /// <summary>Rejects a delivery that waited in the grain queue past its deadline.</summary>
    /// <param name="context">The dispatch context.</param>
    /// <returns>Whether dispatch must be skipped.</returns>
    internal bool Expired(ObserverSubscriberContext context)
    {
        if (context.DeliverBy is not { } deliverBy || clock.GetUtcNow() < deliverBy) return false;
        logger.DeliveryExpiredBeforeDispatch(observer, partition);
        return true;
    }

    /// <summary>Waits for a result while retaining the turn until completion or hold expiry.</summary>
    /// <typeparam name="T">The client result.</typeparam>
    /// <param name="resultTask">The client completion.</param>
    /// <param name="observerResult">Extracts the observer result.</param>
    /// <param name="context">The delivery context.</param>
    /// <returns>The timely result.</returns>
    /// <exception cref="AbandonedDeliveryDiscarded">The result is late or the hold expired.</exception>
    internal async Task<T> For<T>(Task<T> resultTask, Func<T, ObserverSubscriberResult> observerResult, ObserverSubscriberContext context)
    {
        try
        {
            var result = context.AbandonAt is { } abandonAt
                ? await resultTask.WaitAsync(MaximumWait(abandonAt, clock), clock)
                : await resultTask;
            if (context.DeliverBy is { } deliverBy && clock.GetUtcNow() >= deliverBy)
            {
                logger.LateResultForAbandonedDelivery(observer, partition, observerResult(result).State, clock.GetUtcNow() - deliverBy);
                ClientObserverSubscriberMetrics.LateResult(observerResult(result).State);
                throw new AbandonedDeliveryDiscarded();
            }
            return result;
        }
        catch (TimeoutException) when (context.AbandonAt is { } abandonAt && clock.GetUtcNow() >= abandonAt)
        {
            logger.AbandonedDeliveryHoldExpired(observer, partition);
            ClientObserverSubscriberMetrics.HoldExpired();
            throw new AbandonedDeliveryDiscarded();
        }
        finally
        {
            if (context.DeliverBy is { } deliverBy && clock.GetUtcNow() >= deliverBy)
            {
                ClientObserverSubscriberMetrics.FenceDuration(clock.GetUtcNow() - deliverBy);
            }
        }
    }

    static TimeSpan MaximumWait(DateTimeOffset abandonAt, TimeProvider clock)
    {
        var remaining = abandonAt - clock.GetUtcNow();
        return remaining > TimeSpan.Zero ? remaining : TimeSpan.Zero;
    }
}
