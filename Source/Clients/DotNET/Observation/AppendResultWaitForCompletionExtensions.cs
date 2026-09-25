// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Grpc.Core;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.Observation;

/// <summary>
/// Provides extension methods for waiting for observer completion for append operations.
/// </summary>
public static class AppendResultWaitForCompletionExtensions
{
    /// <summary>
    /// Waits for observers handling the appended event types to process up to the append tail sequence number or fail.
    /// </summary>
    /// <param name="appendResult">The append result to wait for observer completion for.</param>
    /// <param name="timeout">Optional server timeout. If none is provided, it defaults to 5 seconds. The client allows an additional 200 milliseconds for the server to report outstanding observers. <see cref="Timeout.InfiniteTimeSpan"/> disables both deadlines.</param>
    /// <returns>An <see cref="AppendResultWaitForCompletionResult"/> describing completion and any failures.</returns>
    /// <exception cref="CannotWaitForObserverCompletion">Thrown when the append result carries no observer surface, which is the case for the in-process testing surfaces.</exception>
    /// <remarks>
    /// An append that never reached the log has nothing to wait for and completes trivially. An append that reached
    /// the log but carries no observer surface is a different thing entirely: completion is unknowable, so it is
    /// reported as a failure by name rather than as success.
    /// </remarks>
    public static async Task<AppendResultWaitForCompletionResult> WaitForCompletion(this IAppendResultForObserverCompletion appendResult, TimeSpan? timeout = default)
    {
        if (appendResult.TailSequenceNumber.IsUnavailable)
        {
            return new(true, []);
        }

        var observers = appendResult.Observers ??
            throw new CannotWaitForObserverCompletion(appendResult.EventStore, appendResult.EventSequenceId);

        timeout ??= TimeSpanFactory.DefaultTimeout();

        var isInfinite = timeout.Value == Timeout.InfiniteTimeSpan;

        // Allow the server a short grace period to return the outstanding observer names at its deadline.
        // Older servers ignore the deadline and fall back to the client cancellation below.
        using var cts = isInfinite
            ? new CancellationTokenSource()
            : new CancellationTokenSource(timeout.Value + TimeSpan.FromMilliseconds(200));

        var eventTypeTails = appendResult is AppendManyResult { AppendedEventTypes.Count: > 0 } batch
            ? batch.AppendedEventTypes.Zip(batch.SequenceNumbers, (type, number) => new Contracts.Observation.AppendedEventTypeTail
            {
                EventType = type.ToContract(),
                SequenceNumber = number
            })
            : appendResult.EventTypes.Select(type => new Contracts.Observation.AppendedEventTypeTail
            {
                EventType = type.ToContract(),
                SequenceNumber = appendResult.TailSequenceNumber
            });

        try
        {
            var response = await observers.WaitForCompletion(
                new Contracts.Observation.WaitForObserverCompletionRequest
                {
                    EventStore = appendResult.EventStore,
                    Namespace = appendResult.EventStoreNamespace,
                    EventSequenceId = appendResult.EventSequenceId,
                    TailEventSequenceNumber = appendResult.TailSequenceNumber,
                    EventTypeTails = eventTypeTails.GroupBy(_ => _.EventType.Id, StringComparer.Ordinal)
                        .Select(_ => _.MaxBy(tail => tail.SequenceNumber)!).ToArray(),
                    TimeoutMilliseconds = isInfinite ? 0 : Math.Max(1, (long)timeout.Value.TotalMilliseconds)
                },
                new CallContext(new CallOptions(cancellationToken: cts.Token)));

            return new(response.IsSuccess, response.FailedPartitions.ToClient())
            {
                TimedOut = response.TimedOut,
                OutstandingObservers = response.OutstandingObservers
            };
        }
        catch (RpcException exception) when (cts.IsCancellationRequested && exception.StatusCode is StatusCode.Cancelled or StatusCode.DeadlineExceeded)
        {
            return new(false, []) { TimedOut = true };
        }
        catch (OperationCanceledException) when (cts.IsCancellationRequested)
        {
            return new(false, []) { TimedOut = true };
        }
    }
}
