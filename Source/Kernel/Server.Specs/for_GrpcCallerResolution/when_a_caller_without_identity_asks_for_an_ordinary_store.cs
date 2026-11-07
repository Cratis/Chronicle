// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Clients;

namespace Cratis.Chronicle.Server.for_GrpcCallerResolution;

public class when_a_caller_without_identity_asks_for_an_ordinary_store : given.a_grpc_host_with_store_entitlements
{
    given.ProbeReply _reply;

    async Task Because() => _reply = await _probe.Decide(new() { Store = Store }, As(null));

    [Fact] void should_resolve_the_caller() => _reply.Owner.ShouldEqual(ConnectionOwner.Unidentified.Subject);
    [Fact] void should_refuse_it() => _reply.Permitted.ShouldBeFalse();
}
