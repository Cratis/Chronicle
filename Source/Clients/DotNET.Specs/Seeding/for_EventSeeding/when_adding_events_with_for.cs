// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Seeding.for_EventSeeding;

public class when_adding_events_with_for : Specification
{
    EventSeeding _seeding;
    EventSourceId _event_source_id;
    TestEvent _first_event;
    TestEvent _second_event;

    void Establish()
    {
        var eventTypes = Substitute.For<IEventTypes>();
        var eventSerializer = Substitute.For<IEventSerializer>();
        var connection = Substitute.For<Connections.IChronicleConnection>();
        var clientArtifactsProvider = Substitute.For<IClientArtifactsProvider>();
        var serviceProvider = Substitute.For<IServiceProvider>();

        eventTypes.GetEventTypeFor(typeof(TestEvent)).Returns(new EventType("test-event", 1));

        _seeding = new EventSeeding(
            "TestEventStore",
            connection,
            eventTypes,
            eventSerializer,
            clientArtifactsProvider,
            serviceProvider,
            Substitute.For<IClientArtifactsActivator>(),
            NullLogger<EventSeeding>.Instance);

        _event_source_id = "test-source-id";
        _first_event = new TestEvent("first");
        _second_event = new TestEvent("second");
    }

    void Because() => _seeding.For(_event_source_id, [_first_event, _second_event]);

    [Fact] void should_return_builder() => _seeding.ShouldBeOfExactType<EventSeeding>();

    record TestEvent(string Value);
}
