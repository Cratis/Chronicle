// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Diagnostics.Load;
using Cratis.Chronicle.Server.for_StreamingCallInterceptor.given;
using Grpc.Core;

namespace Cratis.Chronicle.Server.for_StreamingCallInterceptor.when_handling_a_server_streaming_call;

public class and_the_streams_are_counted : Specification
{
    IKernelLoad _load;
    StreamingCallInterceptor _interceptor;
    int _openedWhileServing;
    ServerCallContext _context;
    RecordingResponseStreamWriter _writer;

    void Establish()
    {
        _load = Substitute.For<IKernelLoad>();
        _interceptor = new(_load);
        _context = Substitute.ForPartsOf<ServerCallContext>();
        _writer = new();
    }

    async Task Because() => await _interceptor.ServerStreamingServerHandler<string, string>(
        "request", _writer, _context, (_, _, _) =>
        {
            _openedWhileServing = _load.ReceivedCalls().Count(call => call.GetMethodInfo().Name == nameof(IKernelLoad.StreamOpened));
            return Task.CompletedTask;
        });

    [Fact] void should_count_the_stream_as_open_while_it_is_served() => _openedWhileServing.ShouldEqual(1);
    [Fact] void should_count_the_stream_as_closed_when_it_ends() => _load.Received(1).StreamClosed();
}
