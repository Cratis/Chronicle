// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Clients;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.Observation.Reducers;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Namespaces;
using Cratis.Chronicle.Observation.ClientSubscriptions;
using Cratis.Chronicle.Recommendations;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Orleans.Core;

namespace Cratis.Chronicle.Observation.Reducers.Clients.for_ClientReducerSubscription.given;

public class a_client_reducer_subscription_with_replay_on_definition_change : Specification
{
    protected ClientReducerSubscription _subscription;
    protected ReducerDefinition _definition;
    protected IObserver _observer;
    protected IOptions<ChronicleOptions> _options;
    protected IRecommendationsManager _recommendations;

    void Establish()
    {
        var definitionComparer = Substitute.For<IReducerDefinitionComparer>();
        definitionComparer
            .Compare(Arg.Any<ReducerKey>(), Arg.Any<ReducerDefinition>(), Arg.Any<ReducerDefinition>())
            .Returns(ReducerDefinitionCompareResult.Different);

        var localSiloDetails = Substitute.For<ILocalSiloDetails>();
        localSiloDetails.SiloAddress.Returns(SiloAddress.FromParsableString("127.0.0.1:11111@1"));

        _options = Substitute.For<IOptions<ChronicleOptions>>();
        _options.Value.Returns(new ChronicleOptions
        {
            Observers = new Observers { DefinitionEvolution = DefinitionEvolutionPolicy.Automatic }
        });

        var namespacesGrain = Substitute.For<INamespaces>();
        namespacesGrain.GetAll().Returns([EventStoreNamespaceName.Default]);
        var connectedClients = Substitute.For<IConnectedClients>();
        connectedClients.IsConnected(Arg.Any<Cratis.Chronicle.Concepts.Clients.ConnectionId>()).Returns(true);
        _observer = Substitute.For<IObserver>();
        _observer.GetSubscription().Returns(ObserverSubscription.Unsubscribed);
        _recommendations = Substitute.For<IRecommendationsManager>();

        // IsActive is false so that SetDefinitionAndSubscribe stops after scheduling replay, which is
        // the only part this specification is about.
        _definition = CreateDefinition("the-read-model");

        // The definition the connection finds stored is another one, so registering this one is a change.
        var storage = Substitute.For<IStorage<ReducerDefinition>>();
        storage.State = CreateDefinition("the-previous-read-model");

        var key = new ConnectedObserverKey("the-reducer", "the-event-store", "the-namespace", EventSequenceId.Log, "the-connection");
        _subscription = new ClientReducerSubscription(
            key,
            ClientSubscriptionFakes.GrainFactory(_observer, connectedClients, namespacesGrain, _recommendations),
            definitionComparer,
            localSiloDetails,
            _options,
            ClientSubscriptionFakes.StorageFor(storage),
            NullLogger<ClientReducerSubscription>.Instance);
    }

    static ReducerDefinition CreateDefinition(string readModel) => new(
        "the-reducer",
        EventSequenceId.Log,
        [],
        readModel,
        false,
        []);
}
