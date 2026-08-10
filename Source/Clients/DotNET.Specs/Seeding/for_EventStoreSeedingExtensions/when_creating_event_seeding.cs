// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Connections;
using Cratis.Chronicle.Events;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Seeding.for_EventStoreSeedingExtensions;

public class when_creating_event_seeding : Specification
{
    IEventStore _eventStore;
    IEventSeeding _existing;
    IEventSeeding _first;
    IEventSeeding _second;

    void Establish()
    {
        _existing = new EventSeeding(
            "testing",
            Substitute.For<IChronicleConnection>(),
            Substitute.For<IEventTypes>(),
            Substitute.For<IEventSerializer>(),
            Substitute.For<IClientArtifactsProvider>(),
            Substitute.For<IServiceProvider>(),
            Substitute.For<IClientArtifactsActivator>(),
            NullLogger<EventSeeding>.Instance);
        _eventStore = Substitute.For<IEventStore>();
        _eventStore.Seeding.Returns(_existing);
    }

    void Because()
    {
        _first = _eventStore.CreateEventSeeding();
        _second = _eventStore.CreateEventSeeding();
    }

    [Fact] void should_create_the_chronicle_implementation() => _first.ShouldBeOfExactType<EventSeeding>();
    [Fact] void should_not_return_the_event_stores_retained_buffer() => _first.ShouldNotEqual(_existing);
    [Fact] void should_create_an_independent_buffer_each_time() => _second.ShouldNotEqual(_first);
}
