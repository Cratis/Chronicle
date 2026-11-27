// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Connections;
using Cratis.Chronicle.Contracts;
using Cratis.Chronicle.Contracts.Commands;
using Cratis.Chronicle.Contracts.Queries;
using Cratis.Chronicle.Contracts.Seeding;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSources;
using Microsoft.Extensions.Logging.Abstractions;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.Seeding.for_EventSeeding.given;

public class a_seeding_builder_that_records_serialization : Specification
{
    protected EventSeeding _seeding;
    protected Contracts.Seeding.IEventSeeding _seedingService;
    protected List<string> _serialized;
    internal List<SeedEventsRequest> _requests;
    IEventSerializer _serializer;

    void Establish()
    {
        _serialized = [];
        _requests = [];
        var connection = Substitute.For<IChronicleConnection, IChronicleServicesAccessor>();
        var services = Substitute.For<IServices>();
        _seedingService = Substitute.For<Contracts.Seeding.IEventSeeding>();
        ((IChronicleServicesAccessor)connection).Services.Returns(services);
        services.Seeding.Returns(_seedingService);
        _seedingService.GetSeedingSupport(Arg.Any<CallContext>()).Returns(QueryResult<EventSeedingSupportResponse>.Success(Guid.NewGuid(), new() { RoutingSupported = true }));
        _seedingService.SeedEvents(Arg.Any<SeedEventsRequest>()).Returns(call =>
        {
            _requests.Add(call.Arg<SeedEventsRequest>());
            return Task.FromResult(CommandResult.Success(Guid.NewGuid()));
        });
        var eventTypes = Substitute.For<IEventTypes>();
        eventTypes.GetEventTypeFor(typeof(Alpha)).Returns(new EventType("alpha", 1));
        eventTypes.GetEventTypeFor(typeof(Beta)).Returns(new EventType("beta", 1));
        _serializer = Substitute.For<IEventSerializer>();
        _serializer.Serialize(Arg.Any<object>()).Returns(call =>
        {
            var value = ((Named)call.Arg<object>()).Value;
            _serialized.Add(value);
            return Task.FromResult(new JsonObject { ["value"] = value });
        });
        _seeding = new EventSeeding(
            "store",
            connection,
            eventTypes,
            _serializer,
            Substitute.For<IClientArtifactsProvider>(),
            Substitute.For<IServiceProvider>(),
            Substitute.For<IClientArtifactsActivator>(),
            NullLogger<EventSeeding>.Instance,
            Substitute.For<IEventSources>());
    }

    protected void SerializeFailsFor(string value) =>
        _serializer.Serialize(Arg.Any<object>()).Returns(call =>
        {
            var named = ((Named)call.Arg<object>()).Value;
            _serialized.Add(named);
            return named == value
                ? Task.FromException<JsonObject>(new InvalidOperationException("cannot serialize"))
                : Task.FromResult(new JsonObject { ["value"] = named });
        });

    protected record Named(string Value);

    [Tag("alpha-tag")]
    protected record Alpha(string Value) : Named(Value);

    protected record Beta(string Value) : Named(Value);
}
