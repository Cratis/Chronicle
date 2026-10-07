// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Grpc.Core;

namespace Cratis.Chronicle.Server.for_StreamingCallInterceptor.when_handling_a_duplex_call;

public class and_the_call_succeeds : Specification
{
    StreamingCallInterceptor _interceptor;
    Exception _error;

    void Establish() => _interceptor = new();

    async Task Because() => _error = await Catch.Exception(() => _interceptor.DuplexStreamingServerHandler<string, string>(
        Substitute.For<IAsyncStreamReader<string>>(),
        Substitute.For<IServerStreamWriter<string>>(),
        Substitute.ForPartsOf<ServerCallContext>(),
        (_, _, _) => Task.CompletedTask));

    [Fact] void should_not_fail() => _error.ShouldBeNull();
}
