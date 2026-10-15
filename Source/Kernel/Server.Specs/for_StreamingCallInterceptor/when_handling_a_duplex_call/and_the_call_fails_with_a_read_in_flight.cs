// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Grpc.Core;

namespace Cratis.Chronicle.Server.for_StreamingCallInterceptor.when_handling_a_duplex_call;

public class and_the_call_fails_with_a_read_in_flight : Specification
{
    StreamingCallInterceptor _interceptor;
    IAsyncStreamReader<string> _requestStream;
    TaskCompletionSource<bool> _pendingRead;
    CancellationToken _readCancellation;
    Task<bool> _readInFlight;
    RpcException _failure;
    Exception _error;

    void Establish()
    {
        _interceptor = new();
        _failure = new(new Status(StatusCode.Internal, "Registration failed"));
        _pendingRead = new(TaskCreationOptions.RunContinuationsAsynchronously);
        _requestStream = Substitute.For<IAsyncStreamReader<string>>();
        _requestStream.MoveNext(Arg.Any<CancellationToken>()).Returns(callInfo =>
        {
            _readCancellation = callInfo.Arg<CancellationToken>();
            _readCancellation.Register(() => _pendingRead.TrySetCanceled(_readCancellation));
            return _pendingRead.Task;
        });
    }

    async Task Because() => _error = await Catch.Exception(() => _interceptor.DuplexStreamingServerHandler<string, string>(
        _requestStream,
        Substitute.For<IServerStreamWriter<string>>(),
        Substitute.ForPartsOf<ServerCallContext>(),
        (requestStream, _, _) =>
        {
            _readInFlight = requestStream.MoveNext(CancellationToken.None);
            return Task.FromException(_failure);
        }));

    [Fact] void should_propagate_the_failure() => _error.ShouldEqual(_failure);
    [Fact] void should_cancel_the_read_in_flight() => _readCancellation.IsCancellationRequested.ShouldBeTrue();
    [Fact] void should_end_the_read_in_flight_before_the_call_ends() => _readInFlight.IsCompleted.ShouldBeTrue();
}
