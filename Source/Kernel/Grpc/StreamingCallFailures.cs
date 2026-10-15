// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Grpc.Core;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.Services;

/// <summary>
/// Extension methods for recording the failure of a streaming call so that it reaches the client.
/// </summary>
/// <remarks>
/// protobuf-net.Grpc ends a call whose response is an <see cref="IObservable{T}"/> successfully when that observable fails
/// before the transport has started waiting for the first response - the failure is dropped, and a client that keeps its
/// request stream open waits for the call forever (https://github.com/Cratis/Chronicle/issues/4537). A service records the
/// failure here as well as passing it to the observer, and the server raises a recorded failure for a call that would
/// otherwise end successfully.
/// </remarks>
internal static class StreamingCallFailures
{
    /// <summary>
    /// The key the failure is recorded under in <see cref="ServerCallContext.UserState"/>.
    /// </summary>
    internal const string Key = "Cratis.Chronicle.StreamingCallFailure";

    /// <summary>
    /// Record the failure of the call.
    /// </summary>
    /// <param name="context">The <see cref="CallContext"/> of the call.</param>
    /// <param name="failure">The failure.</param>
    /// <remarks>Calls that do not come through the gRPC transport, such as in-process calls, have nothing to record on.</remarks>
    public static void RecordFailure(this CallContext context, Exception failure) =>
        context.ServerCallContext?.UserState[Key] = failure;

    /// <summary>
    /// Get the failure recorded for the call, if any.
    /// </summary>
    /// <param name="context">The <see cref="ServerCallContext"/> of the call.</param>
    /// <returns>The recorded failure, or null if none was recorded.</returns>
    public static Exception? GetRecordedFailure(this ServerCallContext context) =>
        context.UserState.TryGetValue(Key, out var failure) ? failure as Exception : null;
}
