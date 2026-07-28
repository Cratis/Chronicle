// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Clients;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.Observation.Reactors;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Namespaces;
using Microsoft.Extensions.Options;
using Orleans.Core;
using Orleans.TestKit;

namespace Cratis.Chronicle.Observation.Reactors.Clients.for_Reactor.given;

public class a_reactor_grain_with_replay_on_definition_change : Specification
{
    protected Reactor _grain;
    protected TestKitSilo _silo;
    protected ReactorDefinition _definition;

    async Task Establish()
    {
        _silo = new TestKitSilo();

        var definitionComparer = Substitute.For<IReactorDefinitionComparer>();
        definitionComparer
            .Compare(Arg.Any<ReactorKey>(), Arg.Any<ReactorDefinition>(), Arg.Any<ReactorDefinition>())
            .Returns(ReactorDefinitionCompareResult.Different);
        _silo.AddService(definitionComparer);

        var localSiloDetails = Substitute.For<ILocalSiloDetails>();
        localSiloDetails.SiloAddress.Returns(SiloAddress.FromParsableString("127.0.0.1:11111@1"));
        _silo.AddService(localSiloDetails);

        _silo.AddService(Options.Create(new ChronicleOptions
        {
            Observers = new Observers { ReplayOnDefinitionChange = true }
        }));

        var namespacesGrain = Substitute.For<INamespaces>();
        namespacesGrain.GetAll().Returns([]);
        _silo.AddProbe(_ => namespacesGrain);
        _silo.AddProbe(_ => Substitute.For<IConnectedClients>());
        _silo.AddProbe(_ => Substitute.For<IObserver>());

        _definition = CreateDefinition(EventSequenceId.Log);

        // ReactorDefinition has no parameterless constructor, so the test silo cannot materialize an
        // initial state on its own. Seed the storage with the definition the grain starts out with.
        var storage = Substitute.For<IStorage<ReactorDefinition>>();
        storage.State = CreateDefinition(EventSequenceId.System);
        _silo.Options.StorageFactory = _ => storage;

        var key = new ConnectedObserverKey("the-reactor", "the-event-store", "the-namespace", EventSequenceId.Log, "the-connection");
        _grain = await _silo.CreateGrainAsync<Reactor>(key.ToString());
    }

    static ReactorDefinition CreateDefinition(EventSequenceId eventSequenceId) => new(
        "the-reactor",
        ReactorOwner.Client,
        eventSequenceId,
        []);
}
