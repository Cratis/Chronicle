// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Clients;
using Cratis.Chronicle.Concepts.Clients;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.Observation.Reactors;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Namespaces;
using Microsoft.Extensions.Options;
using Orleans.Core;
using Orleans.TestKit;

namespace Cratis.Chronicle.Observation.Reactors.Clients.for_Reactor.given;

public class a_reactor_grain_with_a_stored_definition : Specification
{
    protected const string TheConnection = "the-connection";
    protected Reactor _grain;
    protected TestKitSilo _silo;
    protected IObserver _observer;
    protected IConnectedClients _connectedClients;
    protected ConnectedClient _client;

    async Task Establish()
    {
        _silo = new TestKitSilo();

        var definitionComparer = Substitute.For<IReactorDefinitionComparer>();
        definitionComparer
            .Compare(Arg.Any<ReactorKey>(), Arg.Any<ReactorDefinition>(), Arg.Any<ReactorDefinition>())
            .Returns(ReactorDefinitionCompareResult.Same);
        _silo.AddService(definitionComparer);

        var localSiloDetails = Substitute.For<ILocalSiloDetails>();
        localSiloDetails.SiloAddress.Returns(SiloAddress.FromParsableString("127.0.0.1:11111@1"));
        _silo.AddService(localSiloDetails);
        _silo.AddService(Options.Create(new ChronicleOptions()));

        _silo.AddProbe(_ => Substitute.For<INamespaces>());

        _client = new ConnectedClient { ConnectionId = TheConnection, Version = "1.0.0" };
        _connectedClients = Substitute.For<IConnectedClients>();
        _connectedClients.GetConnectedClient(_client.ConnectionId).Returns(_client);
        _silo.AddProbe(_ => _connectedClients);

        _observer = Substitute.For<IObserver>();
        _silo.AddProbe(_ => _observer);

        var definition = new ReactorDefinition("the-reactor", ReactorOwner.Client, EventSequenceId.Log, []);
        var storage = Substitute.For<IStorage<ReactorDefinition>>();
        storage.State = definition;
        _silo.Options.StorageFactory = _ => storage;

        var key = new ConnectedObserverKey("the-reactor", "the-event-store", "the-namespace", EventSequenceId.Log, TheConnection);

        // A fresh activation, as after losing the one the client registered with - the definition is all it has.
        _grain = await _silo.CreateGrainAsync<Reactor>(key.ToString());
    }

    protected Task WasRestored(int times) => _observer.Received(times).RestoreClientSubscription<IReactorObserverSubscriber>(
        ObserverType.Reactor,
        Arg.Any<IEnumerable<Concepts.Events.EventType>>(),
        Arg.Any<SiloAddress>(),
        Arg.Is<ConnectedClient>(client => client.ConnectionId == _client.ConnectionId),
        Arg.Any<bool>(),
        Arg.Any<ObserverFilters?>(),
        Arg.Any<EventGenerationDelivery>());
}
