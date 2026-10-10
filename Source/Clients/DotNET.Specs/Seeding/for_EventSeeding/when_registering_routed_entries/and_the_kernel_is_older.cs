// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Queries;
using Cratis.Chronicle.Contracts.Seeding;
using Grpc.Core;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.Seeding.for_EventSeeding.when_registering_routed_entries;

public class and_the_kernel_is_older : given.a_seeding_builder
{
    Exception _error;

    void Establish()
    {
        _seedingService.GetSeedingSupport(Arg.Any<CallContext>()).Returns(Task.FromException<QueryResult<EventSeedingSupportResponse>>(new RpcException(new Status(StatusCode.Unimplemented, "old kernel"))));
        _seeding.ForEventSource("source", "Lines", "Default", [new TestEvent("value")]);
    }

    async Task Because() => _error = await Catch.Exception(_seeding.Register);

    [Fact] void should_refuse_registration() => _error.ShouldBeOfExactType<EventSeedingRoutingNotSupported>();
    [Fact] async Task should_not_send_any_seed_entries() => await _seedingService.DidNotReceive().SeedEvents(Arg.Any<SeedEventsRequest>(), Arg.Any<CallContext>());
}
