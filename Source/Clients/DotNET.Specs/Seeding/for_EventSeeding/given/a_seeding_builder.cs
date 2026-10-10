// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Connections;
using Cratis.Chronicle.Contracts;
using Cratis.Chronicle.Contracts.Commands;
using Cratis.Chronicle.Contracts.Seeding;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSources;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Seeding.for_EventSeeding.given;

public class a_seeding_builder : Specification
{
    protected EventSeeding _seeding;
    protected IEventSources _eventSources;
    internal SeedEventsRequest _request;

    void Establish()
    {
        var connection = Substitute.For<IChronicleConnection, IChronicleServicesAccessor>();
        var services = Substitute.For<IServices>();
        var seedingService = Substitute.For<Contracts.Seeding.IEventSeeding>();
        ((IChronicleServicesAccessor)connection).Services.Returns(services);
        services.Seeding.Returns(seedingService);
        seedingService.SeedEvents(Arg.Any<SeedEventsRequest>()).Returns(call =>
        {
            _request = call.Arg<SeedEventsRequest>();
            return Task.FromResult(CommandResult.Success(Guid.NewGuid()));
        });
        var eventTypes = Substitute.For<IEventTypes>();
        eventTypes.GetEventTypeFor(typeof(TestEvent)).Returns(new EventType("test-event", 1));
        var serializer = Substitute.For<IEventSerializer>();
        serializer.Serialize(Arg.Any<object>()).Returns(Task.FromResult(new JsonObject()));
        _eventSources = Substitute.For<IEventSources>();
        _seeding = new EventSeeding("store", connection, eventTypes, serializer, Substitute.For<IClientArtifactsProvider>(), Substitute.For<IServiceProvider>(), Substitute.For<IClientArtifactsActivator>(), NullLogger<EventSeeding>.Instance, _eventSources);
    }

    [Tag("static-tag")]
    protected record TestEvent(string Value);
}
