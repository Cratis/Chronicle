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
    internal static KernelConnectionErrorKind ClassifyStreamError(this Exception exception) => Classify(exception, operationCanceledIsCancellation: true);

    /// <summary>
    /// Classify an error thrown while handling an event, looking through wrapping exceptions for the gRPC status that caused it.
    /// </summary>
    /// <param name="exception">The <see cref="Exception"/> to classify.</param>
    /// <returns>The <see cref="KernelConnectionErrorKind"/> for the exception.</returns>
    /// <remarks>
    /// Only a gRPC status counts: an <see cref="OperationCanceledException"/> from handler code, such as a timed out HTTP call,
    /// is a genuine failure.
    /// </remarks>
    internal static KernelConnectionErrorKind ClassifyHandlerError(this Exception exception) => Classify(exception, operationCanceledIsCancellation: false);

    static KernelConnectionErrorKind Classify(Exception exception, bool operationCanceledIsCancellation)
    {
        switch (exception)
        {
            case RpcException { StatusCode: StatusCode.Cancelled }:
                return KernelConnectionErrorKind.Cancelled;

            case RpcException { StatusCode: StatusCode.Unavailable }:
                return KernelConnectionErrorKind.ConnectionLost;

            case RpcException:
                return KernelConnectionErrorKind.Failure;

            case OperationCanceledException when operationCanceledIsCancellation:
                return KernelConnectionErrorKind.Cancelled;

            case AggregateException aggregate when aggregate.InnerExceptions.Count > 0:
                var kinds = aggregate.InnerExceptions.Select(_ => Classify(_, operationCanceledIsCancellation)).ToArray();
                if (kinds.Contains(KernelConnectionErrorKind.Failure))
                {
                    return KernelConnectionErrorKind.Failure;
                }

                return kinds.Contains(KernelConnectionErrorKind.ConnectionLost)
                    ? KernelConnectionErrorKind.ConnectionLost
                    : KernelConnectionErrorKind.Cancelled;
        }

        return exception.InnerException is null
            ? KernelConnectionErrorKind.Failure
            : Classify(exception.InnerException, operationCanceledIsCancellation);
    }
}
