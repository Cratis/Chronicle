// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventTypes;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Contracts.Observation;
using Cratis.Chronicle.Namespaces;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Patterns;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage.Observation;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Services.Observation.for_Observers.given;

public class a_registered_pattern_capture : Cratis.Chronicle.Observation.for_Observer.given.an_observer
{
    protected Contracts.Observation.IObservers _completion;
    protected WaitForObserverCompletionRequest _request;
    protected IObserver _unsubscribedObserver;
    protected IFailedPartitionsStorage _namespaceFailures;
    protected override ObserverId _observerId => PatternCapture.ObserverIdentifier;

    async Task Establish()
    {
        var eventType = new EventType("a-recorded", 1);
        _definitionStorage.State = _definitionStorage.State with
        {
            EventSequenceId = _observerKey.EventSequenceId,
            EventTypes = [eventType],
            Type = Concepts.Observation.ObserverType.Reactor,
            Owner = Concepts.Observation.ObserverOwner.None,
            IsReplayable = false
        };
        await _definitionStorage.WriteStateAsync();
        _eventTypesStorage.GetLatestForAllEventTypes().Returns(
        [
            new EventTypeSchema(eventType, EventTypeOwner.Client, EventTypeSource.Code, new JsonSchema())
        ]);
        var namespaces = Substitute.For<INamespaces>();
        namespaces.GetAll().Returns([_observerKey.Namespace]);
        var grainFactory = Substitute.For<IGrainFactory>();
        grainFactory.GetGrain<INamespaces>(Arg.Any<string>(), Arg.Any<string>()).Returns(namespaces);
        grainFactory.GetGrain<IObserver>(Arg.Any<string>(), Arg.Any<string>()).Returns(_observer);
        var localSiloDetails = Substitute.For<ILocalSiloDetails>();
        localSiloDetails.SiloAddress.Returns(SiloAddress.Zero);
        var capture = new PatternCapture(_storage, localSiloDetails, grainFactory, NullLogger<PatternCapture>.Instance);

        // Startup and event-type registration take this path. The real Observer starts with an
        // existing Owner.None definition and must rewrite it using the actual subscriber type.
        await capture.SubscribeAcrossNamespaces(_observerKey.EventStore);

        var definitions = Substitute.For<IObserverDefinitionsStorage>();
        definitions.GetAll().Returns(_ => [_definitionStorage.State]);
        _eventStoreStorage.Observers.Returns(definitions);
        var states = Substitute.For<IObserverStateStorage>();
        states.GetAll().Returns([]);
        _eventStoreNamespaceStorage.Observers.Returns(states);
        _namespaceFailures = Substitute.For<IFailedPartitionsStorage>();
        _namespaceFailures.GetFor(Arg.Any<IEnumerable<ObserverId>>()).Returns(new Concepts.Observation.FailedPartitions());
        _eventStoreNamespaceStorage.FailedPartitions.Returns(_namespaceFailures);

        // The store-wide definition is visible in a new namespace before its subscription exists.
        _unsubscribedObserver = Substitute.For<IObserver>();
        _unsubscribedObserver.GetSubscription().Returns(ObserverSubscription.Unsubscribed);
        grainFactory.GetGrain<IObserver>(Arg.Any<string>(), Arg.Any<string>()).Returns(_unsubscribedObserver);
        _completion = new Observers(grainFactory, _storage, Substitute.For<IObserverRemover>());
        _request = new()
        {
            EventStore = _observerKey.EventStore,
            Namespace = "second-namespace",
            EventSequenceId = _observerKey.EventSequenceId,
            FirstEventSequenceNumber = 0UL,
            TailEventSequenceNumber = 0UL,
            EventTypeTails = [new AppendedEventTypeTail { EventType = new Contracts.Events.EventType { Id = eventType.Id, Generation = 1 }, SequenceNumber = 0UL }],
            TimeoutMilliseconds = 1
        };
    }
}
