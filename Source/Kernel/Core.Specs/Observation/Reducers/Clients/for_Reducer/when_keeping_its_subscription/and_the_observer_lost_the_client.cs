// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Clients;
using Cratis.Chronicle.Concepts.Clients;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.Observation.Reducers;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Namespaces;
using Microsoft.Extensions.Options;
using Orleans.Core;
using Orleans.TestKit;

namespace Cratis.Chronicle.Observation.Reducers.Clients.for_Reducer.when_keeping_its_subscription;

public class and_the_observer_lost_the_client : Specification
{
    TestKitSilo _silo;
    Reducer _grain;
    IObserver _observer;
    ConnectedClient _client;

    async Task Establish()
    {
        _silo = new TestKitSilo();

        var definitionComparer = Substitute.For<IReducerDefinitionComparer>();
        definitionComparer
            .Compare(Arg.Any<ReducerKey>(), Arg.Any<ReducerDefinition>(), Arg.Any<ReducerDefinition>())
            .Returns(ReducerDefinitionCompareResult.Same);
        _silo.AddService(definitionComparer);

        var localSiloDetails = Substitute.For<ILocalSiloDetails>();
        localSiloDetails.SiloAddress.Returns(SiloAddress.FromParsableString("127.0.0.1:11111@1"));
        _silo.AddService(localSiloDetails);
        _silo.AddService(Options.Create(new ChronicleOptions()));
        _silo.AddService(Substitute.For<Storage.IStorage>());
        _silo.AddProbe(_ => Substitute.For<INamespaces>());

        _client = new ConnectedClient { ConnectionId = "the-connection", Version = "1.0.0" };
        var connectedClients = Substitute.For<IConnectedClients>();
        connectedClients.GetConnectedClient(_client.ConnectionId).Returns(_client);
        _silo.AddProbe(_ => connectedClients);

        _observer = Substitute.For<IObserver>();
        _observer.KnowsClient(_client.ConnectionId).Returns(false);
        _silo.AddProbe(_ => _observer);

        var definition = new ReducerDefinition("the-reducer", EventSequenceId.Log, [], "the-read-model", true, []);
        var storage = Substitute.For<IStorage<ReducerDefinition>>();
        storage.State = definition;
        _silo.Options.StorageFactory = _ => storage;

        var key = new ConnectedObserverKey("the-reducer", "the-event-store", "the-namespace", EventSequenceId.Log, _client.ConnectionId);

        // A fresh activation, as after losing the one the client registered with - the definition is all it has.
        _grain = await _silo.CreateGrainAsync<Reducer>(key.ToString());
    }

    Task Because() => _grain.KeepSubscription();

    [Fact] async Task should_restore_the_subscription_of_the_client() => await _observer.Received(1).RestoreClientSubscription<IReducerObserverSubscriber>(
        ObserverType.Reducer,
        Arg.Any<IEnumerable<EventType>>(),
        Arg.Any<SiloAddress>(),
        Arg.Is<ConnectedClient>(client => client.ConnectionId == _client.ConnectionId),
        Arg.Any<bool>(),
        Arg.Any<ObserverFilters?>(),
        Arg.Any<EventGenerationDelivery>());
}
