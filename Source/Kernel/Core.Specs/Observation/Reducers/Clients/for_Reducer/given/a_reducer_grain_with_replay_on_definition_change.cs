// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Clients;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.Observation.Reducers;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Namespaces;
using Microsoft.Extensions.Options;
using Orleans.Core;
using Orleans.TestKit;

namespace Cratis.Chronicle.Observation.Reducers.Clients.for_Reducer.given;

public class a_reducer_grain_with_replay_on_definition_change : Specification
{
    protected Reducer _grain;
    protected TestKitSilo _silo;
    protected ReducerDefinition _definition;

    async Task Establish()
    {
        _silo = new TestKitSilo();

        var definitionComparer = Substitute.For<IReducerDefinitionComparer>();
        definitionComparer
            .Compare(Arg.Any<ReducerKey>(), Arg.Any<ReducerDefinition>(), Arg.Any<ReducerDefinition>())
            .Returns(ReducerDefinitionCompareResult.Different);
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

        // IsActive is false so that SetDefinitionAndSubscribe stops after scheduling replay, which is
        // the only part this specification is about.
        _definition = CreateDefinition("the-read-model");

        // ReducerDefinition has no parameterless constructor, so the test silo cannot materialize an
        // initial state on its own. Seed the storage with the definition the grain starts out with.
        var storage = Substitute.For<IStorage<ReducerDefinition>>();
        storage.State = CreateDefinition("the-previous-read-model");
        _silo.Options.StorageFactory = _ => storage;

        var key = new ConnectedObserverKey("the-reducer", "the-event-store", "the-namespace", EventSequenceId.Log, "the-connection");
        _grain = await _silo.CreateGrainAsync<Reducer>(key.ToString());
    }

    static ReducerDefinition CreateDefinition(string readModel) => new(
        "the-reducer",
        EventSequenceId.Log,
        [],
        readModel,
        false);
}
