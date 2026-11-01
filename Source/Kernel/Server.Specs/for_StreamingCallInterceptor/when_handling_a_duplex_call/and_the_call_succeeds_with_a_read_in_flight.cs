// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Grpc.Core;

namespace Cratis.Chronicle.Server.for_StreamingCallInterceptor.when_handling_a_duplex_call;

/// <summary>
/// Regression for #4043 / #4537: an ending Observe call must not leave a pending HTTP/2 request-body read.
/// </summary>
public class and_the_call_succeeds_with_a_read_in_flight : given.a_request_stream_with_a_read_that_ends_when_cancelled
{
    Exception _error;

    async Task Because() => _error = await Catch.Exception(() => _interceptor.DuplexStreamingServerHandler<string, string>(
        _requestStream,
        Substitute.For<IServerStreamWriter<string>>(),
        Substitute.ForPartsOf<ServerCallContext>(),
        (requestStream, _, _) =>
        {
            _readInFlight = requestStream.MoveNext(CancellationToken.None);
            return Task.CompletedTask;
        }));

    [Fact] void should_not_fail() => _error.ShouldBeNull();
    [Fact] void should_cancel_the_read_in_flight() => _readCancellation.IsCancellationRequested.ShouldBeTrue();
    [Fact] void should_end_the_read_in_flight_before_the_call_ends() => _readInFlight.IsCompleted.ShouldBeTrue();
}
