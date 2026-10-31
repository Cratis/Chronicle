// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Queries;
using Cratis.Chronicle.Contracts.Seeding;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.Seeding.for_EventSeeding.when_registering_routed_entries;

public class and_the_kernel_does_not_confirm_support : given.a_seeding_builder
{
    Exception _error;

    void Establish()
    {
        _seedingService.GetSeedingSupport(Arg.Any<CallContext>()).Returns(QueryResult<EventSeedingSupportResponse>.Success(Guid.NewGuid(), new() { RoutingSupported = false }));
        _seeding.ForEventSource("legacy", [new TestEvent("legacy")]);
        _seeding.ForNamespace("tenant").ForEventSource("source", "All", "Default", [new TestEvent("routed")], "Order");
    }

    async Task Because() => _error = await Catch.Exception(_seeding.Register);

    [Fact] void should_refuse_registration() => _error.ShouldBeOfExactType<EventSeedingRoutingNotSupported>();
    [Fact] async Task should_not_send_any_seed_entries() => await _seedingService.DidNotReceive().SeedEvents(Arg.Any<SeedEventsRequest>(), Arg.Any<CallContext>());
}
