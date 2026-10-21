// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Queries;
using Cratis.Chronicle.Contracts.Seeding;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.Seeding.for_EventSeeding.when_registering_routed_entries;

public class and_support_is_confirmed_after_a_refusal : given.a_seeding_builder
{
    Exception _error;

    async Task Establish()
    {
        _seeding.ForEventSource("source", "Lines", "line-1", [new TestEvent("value")], "Order");
        _seedingService.GetSeedingSupport(Arg.Any<CallContext>()).Returns(QueryResult<EventSeedingSupportResponse>.Success(Guid.NewGuid(), new() { RoutingSupported = false }));
        _error = await Catch.Exception(_seeding.Register);
        _seedingService.GetSeedingSupport(Arg.Any<CallContext>()).Returns(QueryResult<EventSeedingSupportResponse>.Success(Guid.NewGuid(), new() { RoutingSupported = true }));
    }

    async Task Because() => await _seeding.Register();

    [Fact] void should_have_refused_the_first_attempt() => _error.ShouldBeOfExactType<EventSeedingRoutingNotSupported>();
    [Fact] void should_retain_the_routed_entries_for_retry() => _request.GlobalByEventSource.Single().Entries.Single().EventStreamId.ShouldEqual("line-1");
    [Fact] async Task should_send_the_entries_only_once() => await _seedingService.Received(1).SeedEvents(Arg.Any<SeedEventsRequest>(), Arg.Any<CallContext>());
}
