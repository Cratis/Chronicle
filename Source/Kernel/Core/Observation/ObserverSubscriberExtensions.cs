// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.CompilerServices;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.Webhooks;

namespace Cratis.Chronicle.Observation;

/// <summary>
/// Extension methods for <see cref="IObserverSubscriber"/>.
/// </summary>
public static class ObserverSubscriberExtensions
{
    /// <summary>
    /// The most events handed to an <see cref="IUnpartitionedObserverSubscriber"/> in one call.
    /// </summary>
    /// <remarks>
    /// An unpartitioned subscriber is one activation for every partition, and it handles a batch event by event,
    /// each one a read-modify-write against a document the partitions share. Without a bound on the batch, the time a
    /// call takes grows with the partition, and no timeout is the right size for a partition of any length. With it,
    /// the work behind one answer is bounded, so the timeout means what it says.
    /// </remarks>
    internal const int MaximumEventsPerUnpartitionedCall = 20;

    static readonly ConcurrentDictionary<string, SemaphoreSlim> _unpartitionedGates = new();

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
    /// the same named failure when it elapses. Webhooks cancel and drain their HTTP call before returning. Other
    /// subscribers retain the existing behavior: giving up abandons the wait rather than the work - the subscriber is a
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
        if (subscriber is IUnpartitionedObserverSubscriber)
        {
            return await OnNextInBoundedCalls(subscriber, timeout, partition, events, context, cancellationToken);
        }

        return await OnNextWithinOneCall(subscriber, timeout, partition, events, context, cancellationToken);
    }

    /// <summary>
    /// Hands a whole page to a subscriber without the unpartitioned live-delivery cap.
    /// </summary>
    /// <param name="subscriber">The page subscriber.</param>
    /// <param name="timeout">How long to wait; zero or less waits indefinitely.</param>
    /// <param name="events">The ordered page.</param>
    /// <param name="context">The subscriber context.</param>
    /// <param name="cancellationToken">Cancellation of the wait, not the grain's work.</param>
    /// <returns>The subscriber result.</returns>
    /// <exception cref="SubscriberCallTimedOut">The subscriber did not finish within the timeout.</exception>
    public static async Task<ObserverSubscriberResult> OnNextPageWithin(
        this IObserverPageSubscriber subscriber,
        TimeSpan timeout,
        IEnumerable<AppendedEvent> events,
        ObserverSubscriberContext context,
        CancellationToken cancellationToken = default)
    {
        var started = Stopwatch.GetTimestamp();
        var call = subscriber.OnNextPage(events, context);
        return await WaitForAnswer(call, timeout, ObserverSubscriberKey.AllPartitions, started, cancellationToken);
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

    /// <summary>
    /// Hands the events to an unpartitioned subscriber a bounded chunk at a time, one caller at a time per silo.
    /// </summary>
    /// <param name="subscriber">The <see cref="IObserverSubscriber"/> to hand the events to.</param>
    /// <param name="timeout">How long to wait for each chunk to be answered.</param>
    /// <param name="partition">The <see cref="Key">partition</see> the events belong to.</param>
    /// <param name="events">The <see cref="AppendedEvent">events</see> to hand over.</param>
    /// <param name="context">The <see cref="ObserverSubscriberContext"/> for the call.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> for cancelling the wait.</param>
    /// <returns>The <see cref="ObserverSubscriberResult"/> of the last call made.</returns>
    /// <remarks>
    /// The timeout starts when a chunk is handed over, not when the caller started waiting for its turn: queuing
    /// behind other partitions' calls is the single activation being shared, not a subscriber that is slow to answer.
    /// A failure or a disconnect stops the delivery there, and reports the progress of the chunks before it.
    /// </remarks>
    static async Task<ObserverSubscriberResult> OnNextInBoundedCalls(
        IObserverSubscriber subscriber,
        TimeSpan timeout,
        Key partition,
        IEnumerable<AppendedEvent> events,
        ObserverSubscriberContext context,
        CancellationToken cancellationToken)
    {
        var all = events as IReadOnlyList<AppendedEvent> ?? events.ToArray();
        var gate = _unpartitionedGates.GetOrAdd(GateKeyFor(subscriber), static _ => new SemaphoreSlim(1, 1));
        var lastSuccessful = EventSequenceNumber.Unavailable;

        for (var offset = 0; offset < all.Count || offset == 0; offset += MaximumEventsPerUnpartitionedCall)
        {
            var chunk = all.Skip(offset).Take(MaximumEventsPerUnpartitionedCall).ToArray();
            ObserverSubscriberResult result;
            await gate.WaitAsync(cancellationToken);
            try
            {
                result = await OnNextWithinOneCall(subscriber, timeout, partition, chunk, context, cancellationToken);
            }
            finally
            {
                gate.Release();
            }

            if (result.State != ObserverSubscriberState.Ok)
            {
                return !result.HandledAnyEvents && lastSuccessful.IsActualValue && result.State == ObserverSubscriberState.Failed
                    ? result with { LastSuccessfulObservation = lastSuccessful }
                    : result;
            }

            lastSuccessful = result.LastSuccessfulObservation;
        }

        return ObserverSubscriberResult.Ok(lastSuccessful);
    }

    static string GateKeyFor(IObserverSubscriber subscriber) =>
        subscriber is GrainReference reference
            ? reference.GrainId.ToString()
            : RuntimeHelpers.GetHashCode(subscriber).ToString(CultureInfo.InvariantCulture);

    static async Task<ObserverSubscriberResult> OnNextWithinOneCall(
        IObserverSubscriber subscriber,
        TimeSpan timeout,
        Key partition,
        IEnumerable<AppendedEvent> events,
        ObserverSubscriberContext context,
        CancellationToken cancellationToken)
    {
        if (subscriber is IWebhookObserverSubscriber webhook)
        {
            return await OnNextWebhook(webhook, timeout, partition, events, context, cancellationToken);
        }

        var started = Stopwatch.GetTimestamp();
        var call = subscriber.OnNext(partition, events, context);
        return await WaitForAnswer(call, timeout, partition, started, cancellationToken);
    }

    /// <summary>
    /// Waits for a subscriber call to answer, telling the configured subscriber timeout apart from Orleans giving up on the call.
    /// </summary>
    /// <param name="call">The grain call in flight.</param>
    /// <param name="timeout">The configured subscriber timeout; zero or less waits indefinitely.</param>
    /// <param name="partition">The partition being delivered, for the failure.</param>
    /// <param name="started">The <see cref="Stopwatch"/> timestamp the call was made at.</param>
    /// <param name="cancellationToken">Cancellation of the wait.</param>
    /// <returns>The <see cref="ObserverSubscriberResult"/> the subscriber answered with.</returns>
    /// <exception cref="SubscriberCallTimedOut">The configured subscriber timeout elapsed.</exception>
    /// <exception cref="SubscriberResponseTimedOut">Orleans gave up on the call before the subscriber timeout elapsed.</exception>
    /// <remarks>
    /// Both surface as a <see cref="TimeoutException"/>. The difference is where it comes from: when the call task
    /// itself has faulted, it was Orleans' response timeout; when it is still running, the subscriber timeout elapsed.
    /// </remarks>
    internal static async Task<ObserverSubscriberResult> WaitForAnswer(
        Task<ObserverSubscriberResult> call,
        TimeSpan timeout,
        Key partition,
        long started,
        CancellationToken cancellationToken)
    {
        try
        {
            return timeout <= TimeSpan.Zero
                ? await call.WaitAsync(cancellationToken)
                : await call.WaitAsync(timeout, cancellationToken);
        }
        catch (TimeoutException timeoutException) when (call.IsFaulted)
        {
            throw new SubscriberResponseTimedOut(partition, Stopwatch.GetElapsedTime(started), timeout, timeoutException);
        }
        catch (TimeoutException) when (timeout > TimeSpan.Zero)
        {
            // The call is abandoned rather than cancelled, so nothing is awaiting it any more. Observe whatever it
            // eventually does, or a subscriber that times out and then faults raises an unobserved task exception
            // once the task is collected.
            _ = call.ContinueWith(static abandoned => _ = abandoned.Exception, TaskScheduler.Default);
            throw new SubscriberCallTimedOut(partition, timeout);
        }
    }

    static async Task<ObserverSubscriberResult> OnNextWebhook(
        IWebhookObserverSubscriber subscriber,
        TimeSpan timeout,
        Key partition,
        IEnumerable<AppendedEvent> events,
        ObserverSubscriberContext context,
        CancellationToken cancellationToken)
    {
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        if (timeout > TimeSpan.Zero)
        {
            deadline.CancelAfter(timeout);
        }

        // Orleans can cancel the caller's task before the remote turn has unwound. The non-interleavable
        // Drain turn is a completion barrier on that same activation; do not release a timed-out delivery
        // until it has crossed the barrier.
        ObserverSubscriberResult result;
        try
        {
            result = await subscriber.OnNext(partition, events, context, deadline.Token);
        }
        catch (Exception exception)
        {
            var timedOut = deadline.IsCancellationRequested && !cancellationToken.IsCancellationRequested;
            await deadline.CancelAsync();
            await subscriber.Drain();
            if (exception is OperationCanceledException && timedOut)
            {
                throw new SubscriberCallTimedOut(partition, timeout);
            }

            throw;
        }

        if (deadline.IsCancellationRequested)
        {
            await subscriber.Drain();
        }

        cancellationToken.ThrowIfCancellationRequested();
        if (deadline.IsCancellationRequested)
        {
            throw new SubscriberCallTimedOut(partition, timeout);
        }

        return result;
    }
}
