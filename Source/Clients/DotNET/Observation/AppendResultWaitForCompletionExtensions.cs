// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
    /// Waits for all affected observers to either process up to the append tail sequence number or fail.
    /// </summary>
    /// <param name="appendResult">The append result to wait for observer completion for.</param>
    /// <param name="timeout">Optional timeout. If none is provided, it defaults to 5 seconds.</param>
    /// <returns>An <see cref="AppendResultWaitForCompletionResult"/> describing completion and any failures.</returns>
    public static async Task<AppendResultWaitForCompletionResult> WaitForCompletion(this IAppendResultForObserverCompletion appendResult, TimeSpan? timeout = default)
    {
        if (appendResult.TailSequenceNumber.IsUnavailable)
        {
            return new(true, []);
        }

        var observers = appendResult.Observers;

        if (observers is null)
        {
            return new(true, []);
        }

        timeout ??= TimeSpanFactory.DefaultTimeout();
        using var cts = new CancellationTokenSource(timeout.Value);

        var response = await observers.WaitForCompletion(
            new Contracts.Observation.WaitForObserverCompletionRequest
            {
                EventStore = appendResult.EventStore,
                Namespace = appendResult.EventStoreNamespace,
                EventSequenceId = appendResult.EventSequenceId,
                TailEventSequenceNumber = appendResult.TailSequenceNumber
            },
            new CallContext(new CallOptions(cancellationToken: cts.Token)));

        return new(response.IsSuccess, response.FailedPartitions.ToClient());
    }
}
