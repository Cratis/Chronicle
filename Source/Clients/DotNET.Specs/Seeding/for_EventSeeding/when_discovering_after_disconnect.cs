// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Connections;
using Cratis.Chronicle.Contracts;
using Cratis.Chronicle.Contracts.Seeding;
using Cratis.Chronicle.Events;
using Microsoft.Extensions.Logging.Abstractions;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.Seeding.for_EventSeeding;

public class when_discovering_after_disconnect : Specification
{
    EventSeeding _seeding;
    Contracts.Seeding.IEventSeeding _seedingService;
    ConnectionLifecycle _lifecycle;
    int _seedCallCount;

    void Establish()
    {
        var eventTypes = Substitute.For<IEventTypes>();
        var eventSerializer = Substitute.For<IEventSerializer>();

        _lifecycle = new ConnectionLifecycle(NullLogger<ConnectionLifecycle>.Instance);
        var connection = Substitute.For<IChronicleConnection, IChronicleServicesAccessor>();
        connection.Lifecycle.Returns(_lifecycle);
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

        // Populate entries and discover (sets _discovered = true)
        _seeding.For<TestEvent>("source-1", [new TestEvent("first")]);
        _seeding.Discover().GetAwaiter().GetResult();

        // Register once to confirm entries are sent
        _seeding.Register().GetAwaiter().GetResult();

        // Simulate disconnect — clears _discovered and _entries
        _lifecycle.Disconnected().GetAwaiter().GetResult();

        // Re-populate entries (simulates what an ICanSeedEvents seeder would do on re-discovery)
        _seeding.For<TestEvent>("source-1", [new TestEvent("second")]);
    }

    async Task Because()
    {
        // Re-discover should succeed because _discovered was reset on disconnect
        await _seeding.Discover();
        await _seeding.Register();
    }

    [Fact] void should_send_seed_data_after_rediscovery() => _seedCallCount.ShouldEqual(2);

    record TestEvent(string Value);
}
