// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Services;
using Grpc.Core;

namespace Cratis.Chronicle.Server.for_StreamingCallInterceptor.when_handling_a_duplex_call;

public class and_the_service_recorded_a_failure_the_call_dropped : Specification
{
    StreamingCallInterceptor _interceptor;
    ServerCallContext _context;
    RpcException _failure;
    Exception _error;

    void Establish()
    {
        _interceptor = new();
        _failure = new(new Status(StatusCode.Internal, "Registration failed"));
        _context = Substitute.ForPartsOf<ServerCallContext>();
        _context.UserState[StreamingCallFailures.Key] = _failure;
    }

    async Task Because() => _error = await Catch.Exception(() => _interceptor.DuplexStreamingServerHandler<string, string>(
        Substitute.For<IAsyncStreamReader<string>>(),
        Substitute.For<IServerStreamWriter<string>>(),
        _context,
        (_, _, _) => Task.CompletedTask));

    [Fact] void should_fail_the_call_with_the_recorded_failure() => _error.ShouldEqual(_failure);
}
