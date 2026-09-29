// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Grpc.Core;

namespace Cratis.Chronicle.Observation;

/// <summary>
/// Classifies errors raised while talking to the kernel, so transient connection loss and shutdown are not reported as failures.
/// </summary>
internal static class KernelConnectionErrors
{
    /// <summary>
    /// Classify an error that ended a kernel observation stream, looking through wrapping exceptions.
    /// </summary>
    /// <param name="exception">The <see cref="Exception"/> to classify.</param>
    /// <returns>The <see cref="KernelConnectionErrorKind"/> for the exception.</returns>
    /// <remarks>
    /// Any <see cref="OperationCanceledException"/> counts as cancellation, since the stream is only cancelled by the client itself.
    /// </remarks>
    internal static KernelConnectionErrorKind ClassifyStreamError(this Exception exception)
    {
        switch (exception)
        {
            case RpcException { StatusCode: StatusCode.Cancelled }:
                return KernelConnectionErrorKind.Cancelled;

            case RpcException { StatusCode: StatusCode.Unavailable }:
                return KernelConnectionErrorKind.ConnectionLost;

            case RpcException:
                return KernelConnectionErrorKind.Failure;

            case OperationCanceledException:
                return KernelConnectionErrorKind.Cancelled;

            case AggregateException aggregate when aggregate.InnerExceptions.Count > 0:
                var kinds = aggregate.InnerExceptions.Select(ClassifyStreamError).ToArray();
                if (kinds.Contains(KernelConnectionErrorKind.Failure))
                {
                    return KernelConnectionErrorKind.Failure;
                }

                return kinds.Contains(KernelConnectionErrorKind.ConnectionLost)
                    ? KernelConnectionErrorKind.ConnectionLost
                    : KernelConnectionErrorKind.Cancelled;
        }

        return exception.InnerException?.ClassifyStreamError() ?? KernelConnectionErrorKind.Failure;
    }

    /// <summary>
    /// Check whether an error thrown while handling an event was caused by the observer shutting down.
    /// </summary>
    /// <param name="exception">The <see cref="Exception"/> thrown while handling the event.</param>
    /// <param name="stoppingToken">The token that is cancelled when the observer stops.</param>
    /// <returns>True when the observer is stopping and the error is a cancellation or a lost kernel connection; false otherwise.</returns>
    /// <remarks>
    /// Only the stopping observer decides: handler code can make gRPC calls of its own, so a Cancelled or Unavailable status
    /// from a handler is a genuine failure unless the observer itself is stopping.
    /// </remarks>
    internal static bool IsInterruptedByShutdown(this Exception exception, CancellationToken stoppingToken) =>
        stoppingToken.IsCancellationRequested && exception.ClassifyStreamError() != KernelConnectionErrorKind.Failure;
}
