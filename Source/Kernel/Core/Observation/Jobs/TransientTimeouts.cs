// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Monads;
using Cratis.Orleans.Jobs;
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
    /// The number of times a step that timed out is run before the partition is reported as failed.
    /// </summary>
    internal const int MaximumAttempts = 5;

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
    /// Shortens the messages of a failure to the single line a job step records.
    /// </summary>
    /// <param name="messages">The messages of the failure, outermost first.</param>
    /// <returns>The first message cut down to <see cref="MaximumMessageLength"/>.</returns>
    /// <remarks>
    /// The full messages and the stack trace are kept where the failure is acted on - the failed partition - so every
    /// step that fails does not also carry kilobytes of them in the job's storage.
    /// </remarks>
    internal static IList<string> ShortenForStep(IEnumerable<string> messages)
    {
        var first = messages.FirstOrDefault() ?? string.Empty;
        var firstLine = first.Split('\n', 2)[0].Trim();
        return [firstLine.Length <= MaximumMessageLength ? firstLine : $"{firstLine[..MaximumMessageLength]}..."];
    }

    /// <summary>
    /// Gets the timeout a failed step ended with.
    /// </summary>
    /// <param name="result">The <see cref="JobStepResult"/> of the step.</param>
    /// <param name="jsonSerializerOptions">The serializer options to read the partial result with.</param>
    /// <param name="failure">The <see cref="TransientPartitionFailure"/>, when the step ended with one.</param>
    /// <returns>True if the step failed because it timed out.</returns>
    internal static bool TryGetTransientFailure(JobStepResult result, JsonSerializerOptions jsonSerializerOptions, [NotNullWhen(true)] out TransientPartitionFailure? failure)
    {
        failure = null;
        if (!result.TryGetError(out var error) || error.Cancelled || !error.HasPartialResult)
        {
            return false;
        }

        result.TryGetFullResult<HandleEventsForPartitionResult>(out var handled, out _, jsonSerializerOptions);
        failure = handled?.TransientFailure;
        return failure is not null;
    }

    /// <summary>
    /// Decides whether a step that failed an attempt is run again.
    /// </summary>
    /// <param name="result">The <see cref="JobStepResult"/> of the failed attempt.</param>
    /// <param name="attempt">The number of the attempt that failed, starting at 1.</param>
    /// <param name="jsonSerializerOptions">The serializer options to read the partial result with.</param>
    /// <returns>True if the failure was a timeout and attempts are left.</returns>
    internal static bool ShouldRetry(JobStepResult result, int attempt, JsonSerializerOptions jsonSerializerOptions) =>
        attempt < MaximumAttempts && TryGetTransientFailure(result, jsonSerializerOptions, out _);

    /// <summary>
    /// Reports the partition as failed when the step that ended it ran out of attempts to get past a timeout.
    /// </summary>
    /// <param name="observer">The <see cref="IObserver"/> that owns the partition.</param>
    /// <param name="result">The final <see cref="JobStepResult"/> of the step.</param>
    /// <param name="jsonSerializerOptions">The serializer options to read the partial result with.</param>
    /// <returns>Awaitable task.</returns>
    internal static async Task ReportPartitionFailedWhenAttemptsAreExhausted(IObserver observer, JobStepResult result, JsonSerializerOptions jsonSerializerOptions)
    {
        if (TryGetTransientFailure(result, jsonSerializerOptions, out var failure))
        {
            await observer.PartitionFailed(failure.Partition, failure.FailedAt, failure.Messages, string.Empty, FailureKind.Timeout, context: new(DeliveryFence: failure.DeliveryFence));
        }
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
    /// Reports the handled counts to the observer without letting the report fail the step, sending it again when it
    /// does not get through.
    /// </summary>
    /// <param name="observer">The <see cref="IObserver"/> to report to.</param>
    /// <param name="report">The <see cref="HandledEventsReport"/> to send.</param>
    /// <param name="retryDelay">How long to wait before sending the report again.</param>
    /// <param name="logger">The logger to record retries and an abandoned report with.</param>
    /// <param name="cancellationToken">Cancels waiting to send the report again.</param>
    /// <returns>Awaitable task.</returns>
    /// <remarks>
    /// <para>
    /// The events are already handled when this runs, so failing the step - and every step that follows it - because
    /// the observer did not take a count would trade the statistics being behind for work not being done at all.
    /// </para>
    /// <para>
    /// A report that timed out may still have been taken, so it is sent again with the same identity and the observer
    /// recognizes a repeat; the counts are taken exactly once either way. Only when every attempt fails, or the step is
    /// stopped while waiting, are the counts given up, and that is logged as an error rather than skipped quietly.
    /// </para>
    /// </remarks>
    internal static async Task ReportHandledEventsWithoutFailing(
        IObserver observer,
        HandledEventsReport report,
        TimeSpan retryDelay,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                await observer.ReportHandledEvents(report.Partition, report.Counts, report.ReportedBy, report.HandledUpTo);
                return;
            }
            catch (Exception ex) when (attempt < MaximumAttempts && !cancellationToken.IsCancellationRequested)
            {
                logger.RetryingHandledCountsReport(report.Partition, attempt, ShortMessageFor(ex));
            }
            catch (Exception ex)
            {
                logger.HandledCountsReportAbandoned(report.Partition, report.HandledUpTo, ShortMessageFor(ex));
                return;
            }

            if (!await DelayUnlessCancelled(retryDelay, cancellationToken))
            {
                logger.HandledCountsReportAbandoned(report.Partition, report.HandledUpTo, "the step was stopped");
                return;
            }
        }
    }

    static async Task<bool> DelayUnlessCancelled(TimeSpan delay, CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(delay, cancellationToken);
            return true;
        }
        catch (OperationCanceledException)
        {
            return false;
        }
    }
}
