// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation;

/// <summary>
/// Extension methods for <see cref="IObserverSubscriber"/>.
/// </summary>
public static class ObserverSubscriberExtensions
{
    /// <summary>
    /// Hand a partition's events to the subscriber, giving up on the answer once the timeout elapses.
    /// </summary>
    /// <param name="subscriber">The <see cref="IObserverSubscriber"/> to hand the events to.</param>
    /// <param name="timeout">How long to wait for an answer. Anything at or below zero waits indefinitely.</param>
    /// <param name="partition">The <see cref="Key">partition</see> the events belong to.</param>
    /// <param name="events">The <see cref="AppendedEvent">events</see> to hand over.</param>
    /// <param name="context">The <see cref="ObserverSubscriberContext"/> for the call.</param>
    /// <param name="cancellationToken">Optional <see cref="CancellationToken"/> for cancelling the wait.</param>
    /// <returns>The <see cref="ObserverSubscriberResult"/> the subscriber answered with.</returns>
    /// <exception cref="SubscriberCallTimedOut">Thrown when the subscriber does not answer within the timeout.</exception>
    /// <remarks>
    /// Every path that delivers to a subscriber goes through here, so they all bound the wait the same way and report
    /// the same named failure when it elapses. Giving up abandons the wait rather than the work - the subscriber is a
    /// grain and keeps processing the batch - so this bounds how long the caller is held, not how long the subscriber
    /// runs. That is what makes it safe to shorten: the events are redelivered on the partition's retry, and a
    /// subscriber that eventually finishes the abandoned batch has done work that is idempotent by design.
    /// </remarks>
    public static async Task<ObserverSubscriberResult> OnNextWithin(
        this IObserverSubscriber subscriber,
        TimeSpan timeout,
        Key partition,
        IEnumerable<AppendedEvent> events,
        ObserverSubscriberContext context,
        CancellationToken cancellationToken = default)
    {
        var call = subscriber.OnNext(partition, events, context);
        if (timeout <= TimeSpan.Zero)
        {
            return await call.WaitAsync(cancellationToken);
        }

        try
        {
            return await call.WaitAsync(timeout, cancellationToken);
        }
        catch (TimeoutException)
        {
            throw new SubscriberCallTimedOut(partition, timeout);
        }
    }

    /// <summary>
    /// Gets the keys for the subscriber.
    /// </summary>
    /// <param name="subscriber">The <see cref="IObserverSubscriber"/>.</param>
    /// <returns><see cref="ObserverKey"/> and <see cref="ObserverSubscriberKey"/>.</returns>
    public static (ObserverKey ObserverKey, ObserverSubscriberKey ObserverSubscriberKey) GetKeys(
        this IObserverSubscriber subscriber)
    {
        var subscriberKey = ObserverSubscriberKey.Parse(subscriber.GetPrimaryKeyString());
        var observerKey = new ObserverKey(subscriberKey.ObserverId, subscriberKey.EventStore, subscriberKey.Namespace, subscriberKey.EventSequenceId);
        return (observerKey, subscriberKey);
    }
}
