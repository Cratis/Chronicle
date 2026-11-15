// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Server.for_GrpcCallerResolution;

public class when_an_entitled_caller_asks_for_an_ordinary_store : given.a_grpc_host_with_store_entitlements
{
    given.ProbeReply _reply;

    async Task Because() => _reply = await _probe.Decide(new() { Store = Store }, As(EntitledSubject));

    [Fact] void should_resolve_the_caller() => _reply.Owner.ShouldEqual(EntitledSubject);
    [Fact] void should_allow_it() => _reply.Permitted.ShouldBeTrue();
}
