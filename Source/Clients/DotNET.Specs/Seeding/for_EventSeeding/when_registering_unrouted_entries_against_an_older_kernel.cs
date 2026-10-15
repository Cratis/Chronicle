// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Queries;
using Cratis.Chronicle.Contracts.Seeding;
using Grpc.Core;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.Seeding.for_EventSeeding;

public class when_registering_unrouted_entries_against_an_older_kernel : given.a_seeding_builder
{
    void Establish()
    {
        _seedingService.GetSeedingSupport(Arg.Any<CallContext>()).Returns(Task.FromException<QueryResult<EventSeedingSupportResponse>>(new RpcException(new Status(StatusCode.Unimplemented, "old kernel"))));
        _seeding.ForEventSource("source", [new TestEvent("legacy")]);
        _seeding.ForNamespace("tenant").ForEventSource("source", "All", "Default", [new TestEvent("defaults")]);
        _seeding.ForEvents([new("source", new TestEvent("empty routing")) { EventSourceType = "", EventStreamType = "", EventStreamId = "" }]);
    }

    async Task Because() => await _seeding.Register();

    [Fact] void should_register_global_entries() => _request.GlobalByEventSource.Single().Entries.Count.ShouldEqual(2);
    [Fact] void should_register_namespaced_entries() => _request.NamespacedEntries.Single().ByEventSource.Single().Entries.Count.ShouldEqual(1);
    [Fact] async Task should_not_require_routing_support() => await _seedingService.DidNotReceive().GetSeedingSupport(Arg.Any<CallContext>());
}
