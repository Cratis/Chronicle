// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventTypes;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.EventTypes;
using Cratis.Chronicle.Storage.Observation.Reactors;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Patterns.for_PatternCapture.given;

public class a_pattern_capture : Specification
{
    protected PatternCapture _capture;
    protected IEventTypesStorage _eventTypes;
    protected IReactorDefinitionsStorage _reactors;
    protected IObserver _observer;
    protected EventStoreName _eventStore;
    protected EventStoreNamespaceName _namespace;

    void Establish()
    {
        _eventStore = "some-store";
        _namespace = EventStoreNamespaceName.Default;

        _eventTypes = Substitute.For<IEventTypesStorage>();
        _reactors = Substitute.For<IReactorDefinitionsStorage>();
        _observer = Substitute.For<IObserver>();

        var storage = Substitute.For<IStorage>();
        var eventStoreStorage = Substitute.For<IEventStoreStorage>();
        storage.GetEventStore(_eventStore).Returns(eventStoreStorage);
        eventStoreStorage.EventTypes.Returns(_eventTypes);
        eventStoreStorage.Reactors.Returns(_reactors);

        var localSiloDetails = Substitute.For<ILocalSiloDetails>();
        localSiloDetails.SiloAddress.Returns(SiloAddress.Zero);

        var grainFactory = Substitute.For<IGrainFactory>();
        grainFactory.GetGrain<IObserver>(Arg.Any<string>(), Arg.Any<string>()).Returns(_observer);

        _capture = new(storage, localSiloDetails, grainFactory, NullLogger<PatternCapture>.Instance);
    }

    protected void EventTypesAre(params string[] identifiers) =>
        _eventTypes.GetLatestForAllEventTypes().Returns(
            identifiers.Select(identifier => new EventTypeSchema(
                new EventType(identifier, EventTypeGeneration.First),
                EventTypeOwner.Client,
                EventTypeSource.Code,
                new JsonSchema())).ToArray());
}
