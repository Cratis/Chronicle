// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Clients;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Clients;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.Observation.Reducers;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Namespaces;
using Cratis.Chronicle.Observation.given;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans.Core;
using Orleans.TestKit;

namespace Cratis.Chronicle.Observation.Reducers.Clients.for_Reducer.given;

public class a_reducer_grain_with_a_stored_definition : Specification
{
    protected TestKitSilo _silo;
    protected Reducer _grain;
    protected const string OwnerSequenceMarker = "the-owner-sequence";
    protected IObserver _observer;
    protected IObserver? _ownerObserver;
    protected IReducerDefinitionComparer _definitionComparer;
    protected IConnectedClients _connectedClients;
    protected ConnectedClient _client;
    protected EventType _connectionEventType;
    protected ReducerDefinition _connectionDefinition;
    protected ReducerDefinition _storedDefinition;
    protected IStorage<ReducerDefinition> _stateStorage;
    protected Storage.IStorage _storage;
    protected ILogger<Reducer> _logger;
    protected DeliveryClock _clock;
    protected INamespaces _namespaces;

    async Task Establish()
    {
        _silo = new TestKitSilo();

        _definitionComparer = Substitute.For<IReducerDefinitionComparer>();
        _definitionComparer
            .Compare(Arg.Any<ReducerKey>(), Arg.Any<ReducerDefinition>(), Arg.Any<ReducerDefinition>())
            .Returns(ReducerDefinitionCompareResult.Same);
        _silo.AddService(_definitionComparer);

        var localSiloDetails = Substitute.For<ILocalSiloDetails>();
        localSiloDetails.SiloAddress.Returns(SiloAddress.FromParsableString("127.0.0.1:11111@1"));
        _silo.AddService(localSiloDetails);
        _silo.AddService(Options.Create(new ChronicleOptions()));
        _storage = Substitute.For<Storage.IStorage>();
        _silo.AddService(_storage);
        _logger = Substitute.For<ILogger<Reducer>>();
        _logger.IsEnabled(Arg.Any<LogLevel>()).Returns(true);
        _silo.AddService(_logger);
        _clock = new DeliveryClock();
        _silo.AddService<TimeProvider>(_clock);
        _namespaces = Substitute.For<INamespaces>();
        _silo.AddProbe(_ => _namespaces);

        _client = new ConnectedClient { ConnectionId = "the-connection", Version = "1.0.0" };
        _connectedClients = Substitute.For<IConnectedClients>();
        _connectedClients.GetConnectedClient(_client.ConnectionId).Returns(_client);
        _connectedClients.IsConnected(_client.ConnectionId).Returns(true);
        _silo.AddProbe(_ => _connectedClients);

        _observer = Substitute.For<IObserver>();
        _observer.GetSubscription().Returns(ObserverSubscription.Unsubscribed);
        _observer.KnowsClient(_client.ConnectionId).Returns(false);
        _silo.AddProbe<IObserver>(identity => _ownerObserver is not null && identity.ToString().Contains(OwnerSequenceMarker) ? _ownerObserver : _observer);

        // The stored definition is shared by every connection registering the reducer; here another connection - another
        // client version, as during a rolling deployment - registered last, with different event types.
        var storedEventType = new EventType("stored-event-type", EventTypeGeneration.First);
        _connectionEventType = new EventType("connection-event-type", EventTypeGeneration.First);
        _storedDefinition = new ReducerDefinition("the-reducer", EventSequenceId.Log, [new(storedEventType, WellKnownExpressions.EventSourceId)], "the-read-model", true, []);
        _connectionDefinition = new ReducerDefinition("the-reducer", EventSequenceId.Log, [new(_connectionEventType, WellKnownExpressions.EventSourceId)], "the-read-model", true, []);
        _stateStorage = Substitute.For<IStorage<ReducerDefinition>>();
        _stateStorage.State = _storedDefinition;
        _silo.Options.StorageFactory = _ => _stateStorage;

        var key = new ConnectedObserverKey("the-reducer", "the-event-store", "the-namespace", EventSequenceId.Log, _client.ConnectionId);

        // A fresh activation, as after losing the one the client registered with.
        _grain = await _silo.CreateGrainAsync<Reducer>(key.ToString());
    }

    protected static Task SubscribeOn(IObserver observer) => observer.Subscribe<IReducerObserverSubscriber>(ObserverType.Reducer, Arg.Any<IEnumerable<EventType>>(), Arg.Any<SiloAddress>(), Arg.Any<object?>(), Arg.Any<bool>(), Arg.Any<ObserverFilters?>(), Arg.Any<bool>(), Arg.Any<bool>());

    protected void TheNamespaceExists() => _namespaces.GetAll().Returns([EventStoreNamespaceName.Default]);

    protected Task WasSubscribed(ReducerDefinition definition) => _observer.Received(1).Subscribe<IReducerObserverSubscriber>(
        ObserverType.Reducer,
        Arg.Is<IEnumerable<EventType>>(eventTypes => eventTypes.SequenceEqual(definition.EventTypes.Select(_ => _.EventType))),
        Arg.Any<SiloAddress>(),
        _client,
        filters: definition.Filters);

    protected Task WasRestored(int times) => _observer.Received(times).RestoreClientSubscription<IReducerObserverSubscriber>(
        ObserverType.Reducer,
        Arg.Is<IEnumerable<EventType>>(eventTypes => eventTypes.SequenceEqual(new[] { _connectionEventType })),
        Arg.Any<SiloAddress>(),
        Arg.Is<ConnectedClient>(client => client.ConnectionId == _client.ConnectionId),
        Arg.Any<bool>(),
        Arg.Any<ObserverFilters?>(),
        Arg.Any<EventGenerationDelivery>());
}
