// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Contracts;
using Cratis.Chronicle.Contracts.Seeding;
using Cratis.Chronicle.Events;
using Microsoft.Extensions.Logging.Abstractions;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.Seeding.for_EventSeeding;

public class when_registering_twice : Specification
{
    EventSeeding _seeding;
    Contracts.Seeding.IEventSeeding _seedingService;
    int _seedCallCount;

    void Establish()
    {
        var eventTypes = Substitute.For<IEventTypes>();
        var eventSerializer = Substitute.For<IEventSerializer>();

        var connection = Substitute.For<Connections.IChronicleConnection, IChronicleServicesAccessor>();
        var servicesAccessor = (IChronicleServicesAccessor)connection;
        var services = Substitute.For<IServices>();
        servicesAccessor.Services.Returns(services);

        _seedingService = Substitute.For<Contracts.Seeding.IEventSeeding>();
        services.Seeding.Returns(_seedingService);
        _seedingService.Seed(Arg.Any<SeedRequest>(), Arg.Any<CallContext>()).Returns(Task.CompletedTask);
        _seedingService.When(s => s.Seed(Arg.Any<SeedRequest>(), Arg.Any<CallContext>())).Do(_ => _seedCallCount++);

        eventTypes.GetEventTypeFor(typeof(TestEvent)).Returns(new EventType("test-event", 1));

        eventSerializer.Serialize(Arg.Any<object>()).Returns(callInfo =>
            Task.FromResult(new JsonObject { ["value"] = "serialized" }));

        _seeding = new EventSeeding(
            "TestEventStore",
            "TestNamespace",
            connection,
            eventTypes,
            eventSerializer,
            Substitute.For<IClientArtifactsProvider>(),
            Substitute.For<IServiceProvider>(),
            Substitute.For<IClientArtifactsActivator>(),
            NullLogger<EventSeeding>.Instance);

        _seeding.For<TestEvent>("source-1", [new TestEvent("first")]);
    }

    async Task Because()
    {
        await _seeding.Register();
        await _seeding.Register();
    }

    [Fact] void should_send_entries_on_both_calls() => _seedCallCount.ShouldEqual(2);

    record TestEvent(string Value);
}
