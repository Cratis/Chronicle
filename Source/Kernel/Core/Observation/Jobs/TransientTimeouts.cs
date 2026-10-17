// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using Cratis.Monads;
using Microsoft.Extensions.Logging;
using OneOf.Types;

namespace Cratis.Chronicle.Observation.Jobs;

/// <summary>
/// Helpers for treating a timed out grain call as transient rather than as the partition failing.
/// </summary>
/// <remarks>
/// A grain call that outlives its response timeout says the callee was too busy to answer, not that handling the
/// events went wrong. Escalating it to <see cref="IObserver.PartitionFailed"/> sends one more call to the very grain
/// that is already behind, which times out too, and turns a slow grain into a cascade of failed partitions.
/// </remarks>
internal static class TransientTimeouts
{
    /// <summary>
    /// The longest a recorded failure message is allowed to be.
    /// </summary>
    internal const int MaximumMessageLength = 240;

    /// <summary>
    /// Gets the default delay before a handling attempt that timed out is made again.
    /// </summary>
    internal static readonly TimeSpan DefaultRetryDelay = TimeSpan.FromSeconds(2);

    /// <summary>
    /// Gets whether the exception, or anything it wraps, is a <see cref="TimeoutException"/>.
    /// </summary>
    /// <param name="exception">The <see cref="Exception"/> to inspect.</param>
    /// <returns>True if it is a timeout.</returns>
    internal static bool IsTimeout(Exception exception) => exception.ToFailureKind() == FailureKind.Timeout;

    /// <summary>
    /// Creates a short, single-line description of a failure that does not carry the stack trace or the grain's status dump.
    /// </summary>
    /// <param name="exception">The <see cref="Exception"/> to describe.</param>
    /// <returns>The short message.</returns>
    internal static string ShortMessageFor(Exception exception)
    {
        var message = exception.GetBaseException().Message;
        var firstLine = message.Split('\n', 2)[0].Trim();
        return firstLine.Length <= MaximumMessageLength ? firstLine : $"{firstLine[..MaximumMessageLength]}...";
    }

    /// <summary>
    /// Runs a handling attempt and, when it timed out, makes it once more after a delay before giving up on it.
    /// </summary>
    /// <typeparam name="TResult">The type of the result of the attempt.</typeparam>
    /// <param name="attempt">The attempt to make.</param>
    /// <param name="delay">How long to wait before the second attempt.</param>
    /// <param name="onRetry">Called when the attempt is retried, with the timeout that caused it.</param>
    /// <param name="cancellationToken">Cancels the wait.</param>
    /// <returns>The result of the last attempt made.</returns>
    /// <remarks>
    /// The wait runs in the step's background work, never in an activation turn.
    /// </remarks>
    internal static async Task<Catch<TResult, None>> RetryOnceOnTimeout<TResult>(
        Func<Task<Catch<TResult, None>>> attempt,
        TimeSpan delay,
        Action<Exception> onRetry,
        CancellationToken cancellationToken)
    {
        var result = await attempt();
        if (!result.TryGetException(out var exception) || !IsTimeout(exception) || cancellationToken.IsCancellationRequested)
        {
            return result;
        }

        onRetry(exception);
        await Task.Delay(delay, cancellationToken);
        return await attempt();
    }

    /// <summary>
    /// Reports the handled counts to the observer without letting a timeout fail the step.
    /// </summary>
    /// <param name="observer">The <see cref="IObserver"/> to report to.</param>
    /// <param name="partition">The partition the counts are for.</param>
    /// <param name="counts">The counts per event type.</param>
    /// <param name="logger">The logger to record a skipped report with.</param>
    /// <returns>Awaitable task.</returns>
    /// <remarks>
    /// The counts are statistics. Failing a step - and every step that follows it - because the observer was too busy
    /// to take a count would trade the statistics being a little behind for work not being done at all.
    /// </remarks>
    internal static async Task ReportHandledEventsWithoutFailing(
        IObserver observer,
        Concepts.Keys.Key partition,
        IReadOnlyDictionary<Concepts.Events.EventTypeId, Concepts.Events.EventCount> counts,
        ILogger logger)
    {
        try
        {
            await observer.ReportHandledEvents(partition, counts);
        }
        catch (Exception ex) when (IsTimeout(ex))
        {
            logger.HandledCountsReportTimedOut(partition, ShortMessageFor(ex));
        }
    }
}
