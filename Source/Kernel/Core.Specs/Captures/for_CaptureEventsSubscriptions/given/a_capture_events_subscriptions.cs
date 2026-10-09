// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Captures;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventTypes;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Namespaces;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.Captures;
using Cratis.Chronicle.Storage.EventTypes;
using Cratis.Chronicle.Storage.Observation;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Captures.for_CaptureEventsSubscriptions.given;

public class a_capture_events_subscriptions : Specification
{
    protected const string Sequence = "inbox-fulfillment";
    protected static readonly EventStoreName _eventStore = "some-store";

    protected CaptureEventsSubscriptions _subscriptions;
    protected IStorage _storage;
    protected IEventStoreStorage _eventStoreStorage;
    protected IEventTypesStorage _eventTypes;
    protected ICapturesStorage _captures;
    protected IObserverDefinitionsStorage _observers;
    protected IGrainFactory _grainFactory;
    protected Dictionary<string, IObserver> _observersByNamespace;
    protected CaptureDefinition _definition;
    protected string[] _namespaces = ["first", "second"];

    void Establish()
    {
        _storage = Substitute.For<IStorage>();
        _eventStoreStorage = Substitute.For<IEventStoreStorage>();
        _eventTypes = Substitute.For<IEventTypesStorage>();
        _captures = Substitute.For<ICapturesStorage>();
        _observers = Substitute.For<IObserverDefinitionsStorage>();
        _storage.GetEventStore(_eventStore).Returns(_eventStoreStorage);
        _eventStoreStorage.EventTypes.Returns(_eventTypes);
        _eventStoreStorage.Captures.Returns(_captures);
        _eventStoreStorage.Observers.Returns(_observers);

        _grainFactory = Substitute.For<IGrainFactory>();
        var namespaces = Substitute.For<INamespaces>();
        namespaces.GetAll().Returns(_ => _namespaces.Select(name => (EventStoreNamespaceName)name).ToArray());
        _grainFactory.GetGrain<INamespaces>(Arg.Any<string>(), Arg.Any<string>()).Returns(namespaces);

        _observersByNamespace = [];
        _definition = new CaptureDefinition(
            CaptureId.New(),
            "Shipments",
            new SourceDefinition(SourceType.Events, Sequence: Sequence, Events: ["ShipmentDispatched", "ShipmentDelivered"]),
            string.Empty,
            null,
            [],
            [],
            []);

        RegisterPublic("ShipmentDispatched", "fulfillment");
        RegisterPublic("ShipmentDelivered", "fulfillment");
        _eventTypes.GetFor(Arg.Any<IEnumerable<EventTypeId>>()).Returns(call =>
            call.Arg<IEnumerable<EventTypeId>>().Select(id => _schemas[id.Value]).ToArray());

        _grainFactory.GetGrain<IObserver>(Arg.Any<string>(), Arg.Any<string>()).Returns(call =>
        {
            var key = ObserverKey.Parse((string)call[0]);
            if (!_observersByNamespace.TryGetValue(key.Namespace.Value, out var observer))
            {
                _observersByNamespace[key.Namespace.Value] = observer = Substitute.For<IObserver>();
                _keys[key.Namespace.Value] = key;
            }

            return observer;
        });

        _subscriptions = new(_storage, Substitute.For<ILocalSiloDetails>(), _grainFactory, NullLogger<CaptureEventsSubscriptions>.Instance);
    }

    protected Dictionary<string, ObserverKey> _keys = [];

    protected static Task<IEnumerable<EventType>> AnySubscribe(IObserver observer) =>
        observer.SubscribeAdditively<ICaptureEventsSubscriber>(Arg.Any<ObserverType>(), Arg.Any<IEnumerable<EventType>>(), Arg.Any<SiloAddress>(), Arg.Any<object?>(), Arg.Any<bool>(), Arg.Any<ObserverFilters?>());

    protected static Task<IEnumerable<EventType>> AnyRecover(IObserver observer) =>
        observer.RecoverStalledSubscription<ICaptureEventsSubscriber>(Arg.Any<ObserverType>(), Arg.Any<IEnumerable<EventType>>(), Arg.Any<SiloAddress>(), Arg.Any<object?>(), Arg.Any<bool>(), Arg.Any<ObserverFilters?>());

    protected static Task<bool> AnyNeedsRecovery(IObserver observer) =>
        observer.NeedsSubscriptionRecovery(Arg.Any<IEnumerable<EventType>>());

    readonly Dictionary<string, EventTypeSchema> _schemas = [];

    protected void RegisterPublic(string name, string origin) =>
        _schemas[name] = new EventTypeSchema(new EventType(name, EventTypeGeneration.First), EventTypeOwner.Client, EventTypeSource.Code, new JsonSchema(), EventTypeVisibility.Public, origin);
}
