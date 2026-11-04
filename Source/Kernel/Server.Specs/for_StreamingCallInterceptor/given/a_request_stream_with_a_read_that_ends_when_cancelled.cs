// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Grpc.Core;

namespace Cratis.Chronicle.Server.for_StreamingCallInterceptor.given;

public class a_request_stream_with_a_read_that_ends_when_cancelled : Specification
{
    internal StreamingCallInterceptor _interceptor;
    protected IAsyncStreamReader<string> _requestStream;
    protected TaskCompletionSource<bool> _pendingRead;
    protected CancellationToken _readCancellation;
    protected Task<bool> _readInFlight;

    void Establish()
    {
        _interceptor = new();
        _pendingRead = new(TaskCreationOptions.RunContinuationsAsynchronously);
        _requestStream = Substitute.For<IAsyncStreamReader<string>>();
        _requestStream.MoveNext(Arg.Any<CancellationToken>()).Returns(callInfo =>
        {
            _readCancellation = callInfo.Arg<CancellationToken>();
            _readCancellation.Register(() => _pendingRead.TrySetCanceled(_readCancellation));
            return _pendingRead.Task;
        });
    }
}
