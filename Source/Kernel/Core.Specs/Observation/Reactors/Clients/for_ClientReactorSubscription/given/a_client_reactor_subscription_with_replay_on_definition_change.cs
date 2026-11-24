// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Clients;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.Observation.Reactors;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Namespaces;
using Cratis.Chronicle.Observation.ClientSubscriptions;
using Cratis.Chronicle.Recommendations;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Orleans.Core;

namespace Cratis.Chronicle.Observation.Reactors.Clients.for_ClientReactorSubscription.given;

public class a_client_reactor_subscription_with_replay_on_definition_change : Specification
{
    protected ClientReactorSubscription _subscription;
    protected ReactorDefinition _definition;
    protected IObserver _observerProbe;
    protected IOptions<ChronicleOptions> _options;
    protected INamespaces _namespacesGrain;
    protected IRecommendationsManager _recommendationsProbe;

    void Establish()
    {
        var definitionComparer = Substitute.For<IReactorDefinitionComparer>();
        definitionComparer
            .Compare(Arg.Any<ReactorKey>(), Arg.Any<ReactorDefinition>(), Arg.Any<ReactorDefinition>())
            .Returns(ReactorDefinitionCompareResult.Different);

        var localSiloDetails = Substitute.For<ILocalSiloDetails>();
        localSiloDetails.SiloAddress.Returns(SiloAddress.FromParsableString("127.0.0.1:11111@1"));

        _options = Substitute.For<IOptions<ChronicleOptions>>();
        _options.Value.Returns(new ChronicleOptions
        {
            Observers = new Observers { ReplayOnDefinitionChange = true }
        });

        _namespacesGrain = Substitute.For<INamespaces>();
        _namespacesGrain.GetAll().Returns([EventStoreNamespaceName.Default]);
        var connectedClients = Substitute.For<IConnectedClients>();
        connectedClients.IsConnected(Arg.Any<Cratis.Chronicle.Concepts.Clients.ConnectionId>()).Returns(true);
        _observerProbe = Substitute.For<IObserver>();
        _observerProbe.GetSubscription().Returns(ObserverSubscription.Unsubscribed);
        _recommendationsProbe = Substitute.For<IRecommendationsManager>();

        _definition = CreateDefinition(EventSequenceId.Log);

        // The definition the connection finds stored is another one, so registering this one is a change.
        var storage = Substitute.For<IStorage<ReactorDefinition>>();
        storage.State = CreateDefinition(EventSequenceId.System);

        var key = new ConnectedObserverKey("the-reactor", "the-event-store", "the-namespace", EventSequenceId.Log, "the-connection");
        _subscription = new ClientReactorSubscription(
            key,
            ClientSubscriptionFakes.GrainFactory(_observerProbe, connectedClients, _namespacesGrain, _recommendationsProbe),
            definitionComparer,
            localSiloDetails,
            _options,
            ClientSubscriptionFakes.StorageFor(storage),
            NullLogger<ClientReactorSubscription>.Instance);
    }

    static ReactorDefinition CreateDefinition(EventSequenceId eventSequenceId) => new(
        "the-reactor",
        ReactorOwner.Client,
        eventSequenceId,
        []);
}
