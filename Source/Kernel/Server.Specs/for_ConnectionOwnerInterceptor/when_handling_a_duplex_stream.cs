// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Grpc.Core;

namespace Cratis.Chronicle.Server.for_ConnectionOwnerInterceptor;

public class when_handling_a_duplex_stream : given.an_interceptor
{
    Task Because() => _interceptor.DuplexStreamingServerHandler<string, string>(Substitute.For<IAsyncStreamReader<string>>(), Substitute.For<IServerStreamWriter<string>>(), _context, async (_, _, _) =>
    {
        await Task.Yield();
        _duringCall = _owners.Principal;
    });

    [Fact] void should_capture_the_callers_principal() => _duringCall.ShouldBeSame(_caller);
    [Fact] void should_clear_the_holder() => _owners.Principal.ShouldBeNull();
}
