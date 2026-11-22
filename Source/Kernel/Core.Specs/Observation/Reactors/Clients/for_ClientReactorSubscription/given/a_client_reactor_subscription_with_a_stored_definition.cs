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
using Cratis.Chronicle.Observation.ClientSubscriptions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Orleans.Core;

namespace Cratis.Chronicle.Observation.Reactors.Clients.for_ClientReactorSubscription.given;

public class a_client_reactor_subscription_with_a_stored_definition : Specification
{
    protected const string TheConnection = "the-connection";
    protected ClientReactorSubscription _subscription;
    protected IObserver _observer;
    protected IReactorDefinitionComparer _definitionComparer;
    protected IConnectedClients _connectedClients;
    protected ConnectedClient _client;
    protected EventType _storedEventType;
    protected EventType _connectionEventType;
    protected ReactorDefinition _connectionDefinition;
    protected ReactorDefinition _storedDefinition;
    protected IStorage<ReactorDefinition> _stateStorage;

    void Establish()
    {
        _definitionComparer = Substitute.For<IReactorDefinitionComparer>();
        _definitionComparer
            .Compare(Arg.Any<ReactorKey>(), Arg.Any<ReactorDefinition>(), Arg.Any<ReactorDefinition>())
            .Returns(ReactorDefinitionCompareResult.Same);

        var localSiloDetails = Substitute.For<ILocalSiloDetails>();
        localSiloDetails.SiloAddress.Returns(SiloAddress.FromParsableString("127.0.0.1:11111@1"));

        _client = new ConnectedClient { ConnectionId = TheConnection, Version = "1.0.0" };
        _connectedClients = Substitute.For<IConnectedClients>();
        _connectedClients.GetConnectedClient(Arg.Any<ConnectionId>()).Returns(_client);
        _connectedClients.IsConnected(Arg.Any<ConnectionId>()).Returns(true);

        _observer = Substitute.For<IObserver>();
        _observer.GetSubscription().Returns(ObserverSubscription.Unsubscribed);

        // The stored definition is shared by every connection registering the reactor; here another connection - another
        // client version, as during a rolling deployment - registered last, with different event types.
        _storedEventType = new EventType("stored-event-type", EventTypeGeneration.First);
        _connectionEventType = new EventType("connection-event-type", EventTypeGeneration.First);
        _storedDefinition = new ReactorDefinition("the-reactor", ReactorOwner.Client, EventSequenceId.Log, [new(_storedEventType, WellKnownExpressions.EventSourceId)]);
        _connectionDefinition = new ReactorDefinition("the-reactor", ReactorOwner.Client, EventSequenceId.Log, [new(_connectionEventType, WellKnownExpressions.EventSourceId)]);
        _stateStorage = Substitute.For<IStorage<ReactorDefinition>>();
        _stateStorage.State = _storedDefinition;

        var key = new ConnectedObserverKey("the-reactor", "the-event-store", "the-namespace", EventSequenceId.Log, TheConnection);

        // A fresh subscription, as after losing the one the client registered with.
        _subscription = new ClientReactorSubscription(
            key,
            ClientSubscriptionFakes.GrainFactory(_observer, _connectedClients, Substitute.For<INamespaces>()),
            _definitionComparer,
            localSiloDetails,
            Options.Create(new ChronicleOptions()),
            ClientSubscriptionFakes.StorageFor(_stateStorage),
            NullLogger<ClientReactorSubscription>.Instance);
    }

    protected Task WasSubscribed(ReactorDefinition definition) => _observer.Received(1).Subscribe<IReactorObserverSubscriber>(
        ObserverType.Reactor,
        Arg.Is<EventTypeSet>(eventTypes => eventTypes.SequenceEqual(definition.EventTypes.Select(_ => _.EventType))),
        Arg.Any<SiloAddress>(),
        _client,
        definition.IsReplayable,
        definition.Filters);

    protected Task WasRestored(int times) => _observer.Received(times).RestoreClientSubscription<IReactorObserverSubscriber>(
        ObserverType.Reactor,
        Arg.Is<EventTypeSet>(eventTypes => eventTypes.SequenceEqual(new[] { _connectionEventType })),
        Arg.Any<SiloAddress>(),
        Arg.Is<ConnectedClient>(client => client.ConnectionId == _client.ConnectionId),
        Arg.Any<bool>(),
        Arg.Any<ObserverFilters?>(),
        Arg.Any<EventGenerationDelivery>());
}
