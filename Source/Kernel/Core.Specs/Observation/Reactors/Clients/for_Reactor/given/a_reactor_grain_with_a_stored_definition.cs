// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Clients;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Clients;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.Observation.Reactors;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Namespaces;
using Cratis.Chronicle.Observation.given;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans.Core;
using Orleans.TestKit;

namespace Cratis.Chronicle.Observation.Reactors.Clients.for_Reactor.given;

public class a_reactor_grain_with_a_stored_definition : Specification
{
    protected const string TheConnection = "the-connection";
    protected Reactor _grain;
    protected TestKitSilo _silo;
    protected const string OwnerSequenceMarker = "the-owner-sequence";
    protected IObserver _observer;
    protected IObserver? _ownerObserver;
    protected IReactorDefinitionComparer _definitionComparer;
    protected IConnectedClients _connectedClients;
    protected ConnectedClient _client;
    protected EventType _storedEventType;
    protected EventType _connectionEventType;
    protected ReactorDefinition _connectionDefinition;
    protected ReactorDefinition _storedDefinition;
    protected IStorage<ReactorDefinition> _stateStorage;
    protected Storage.IStorage _storage;
    protected ILogger<Reactor> _logger;
    protected DeliveryClock _clock;
    protected INamespaces _namespaces;

    async Task Establish()
    {
        _silo = new TestKitSilo();

        _definitionComparer = Substitute.For<IReactorDefinitionComparer>();
        _definitionComparer
            .Compare(Arg.Any<ReactorKey>(), Arg.Any<ReactorDefinition>(), Arg.Any<ReactorDefinition>())
            .Returns(ReactorDefinitionCompareResult.Same);
        _silo.AddService(_definitionComparer);

        var localSiloDetails = Substitute.For<ILocalSiloDetails>();
        localSiloDetails.SiloAddress.Returns(SiloAddress.FromParsableString("127.0.0.1:11111@1"));
        _silo.AddService(localSiloDetails);
        _silo.AddService(Options.Create(new ChronicleOptions()));
        _storage = Substitute.For<Storage.IStorage>();
        _silo.AddService(_storage);
        _logger = Substitute.For<ILogger<Reactor>>();
        _logger.IsEnabled(Arg.Any<LogLevel>()).Returns(true);
        _silo.AddService(_logger);
        _clock = new DeliveryClock();
        _silo.AddService<TimeProvider>(_clock);

        _namespaces = Substitute.For<INamespaces>();
        _silo.AddProbe(_ => _namespaces);

        _client = new ConnectedClient { ConnectionId = TheConnection, Version = "1.0.0" };
        _connectedClients = Substitute.For<IConnectedClients>();
        _connectedClients.GetConnectedClient(_client.ConnectionId).Returns(_client);
        _connectedClients.IsConnected(_client.ConnectionId).Returns(true);
        _silo.AddProbe(_ => _connectedClients);

        _observer = Substitute.For<IObserver>();
        _observer.GetSubscription().Returns(ObserverSubscription.Unsubscribed);
        _silo.AddProbe<IObserver>(identity => _ownerObserver is not null && identity.ToString().Contains(OwnerSequenceMarker) ? _ownerObserver : _observer);

        // The stored definition is shared by every connection registering the reactor; here another connection - another
        // client version, as during a rolling deployment - registered last, with different event types.
        _storedEventType = new EventType("stored-event-type", EventTypeGeneration.First);
        _connectionEventType = new EventType("connection-event-type", EventTypeGeneration.First);
        _storedDefinition = new ReactorDefinition("the-reactor", ReactorOwner.Client, EventSequenceId.Log, [new(_storedEventType, WellKnownExpressions.EventSourceId)]);
        _connectionDefinition = new ReactorDefinition("the-reactor", ReactorOwner.Client, EventSequenceId.Log, [new(_connectionEventType, WellKnownExpressions.EventSourceId)]);
        _stateStorage = Substitute.For<IStorage<ReactorDefinition>>();
        _stateStorage.State = _storedDefinition;
        _silo.Options.StorageFactory = _ => _stateStorage;

        var key = new ConnectedObserverKey("the-reactor", "the-event-store", "the-namespace", EventSequenceId.Log, TheConnection);

        // A fresh activation, as after losing the one the client registered with.
        _grain = await _silo.CreateGrainAsync<Reactor>(key.ToString());
    }

    protected static Task SubscribeOn(IObserver observer) => observer.Subscribe<IReactorObserverSubscriber>(ObserverType.Reactor, Arg.Any<IEnumerable<EventType>>(), Arg.Any<SiloAddress>(), Arg.Any<object?>(), Arg.Any<bool>(), Arg.Any<ObserverFilters?>(), Arg.Any<bool>(), Arg.Any<bool>());

    protected void TheNamespaceExists() => _namespaces.GetAll().Returns([EventStoreNamespaceName.Default]);

    protected Task WasSubscribed(ReactorDefinition definition) => _observer.Received(1).Subscribe<IReactorObserverSubscriber>(
        ObserverType.Reactor,
        Arg.Is<IEnumerable<EventType>>(eventTypes => eventTypes.SequenceEqual(definition.EventTypes.Select(_ => _.EventType))),
        Arg.Any<SiloAddress>(),
        _client,
        definition.IsReplayable,
        definition.Filters);

    protected Task WasRestored(int times) => _observer.Received(times).RestoreClientSubscription<IReactorObserverSubscriber>(
        ObserverType.Reactor,
        Arg.Is<IEnumerable<EventType>>(eventTypes => eventTypes.SequenceEqual(new[] { _connectionEventType })),
        Arg.Any<SiloAddress>(),
        Arg.Is<ConnectedClient>(client => client.ConnectionId == _client.ConnectionId),
        Arg.Any<bool>(),
        Arg.Any<ObserverFilters?>(),
        Arg.Any<EventGenerationDelivery>());
}
