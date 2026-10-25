// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Clients;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Clients;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.Observation.Reducers;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Concepts.Sinks;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Namespaces;
using Cratis.Chronicle.Recommendations;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage.Sinks;
using Microsoft.Extensions.Options;
using Orleans.Core;
using Orleans.TestKit;

namespace Cratis.Chronicle.Observation.Reducers.Clients.for_Reducer.given;

/// <summary>
/// An active reducer whose definition is unchanged, over a sink that reports its composite keys are in a legacy key format.
/// </summary>
public class a_reducer_grain_with_a_legacy_composite_table : Specification
{
    protected TestKitSilo _silo;
    protected Reducer _grain;
    protected ReducerDefinition _definition;
    protected IObserver _observer;
    protected IRecommendationsManager _recommendations;

    /// <summary>
    /// Gets the definition evolution policy the kernel is configured with.
    /// </summary>
    protected virtual DefinitionEvolutionPolicy Policy => DefinitionEvolutionPolicy.Automatic;

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
        _silo.AddService(Options.Create(new ChronicleOptions { Observers = new Observers { DefinitionEvolution = Policy } }));

        var readModel = new ReadModelDefinition(
            "the-read-model",
            "the_read_model",
            "TheReadModel",
            ReadModelOwner.Client,
            ReadModelSource.Code,
            ReadModelObserverType.Reducer,
            ReadModelObserverIdentifier.Unspecified,
            SinkDefinition.None,
            new Dictionary<ReadModelGeneration, JsonSchema> { [ReadModelGeneration.First] = await JsonSchema.FromJsonAsync("{\"type\":\"object\"}") },
            []);
        var sink = Substitute.For<ISink, ICanReportKeyFormatMigration>();
        ((ICanReportKeyFormatMigration)sink).RequiresKeyFormatMigration().Returns(true);
        var storage = Substitute.For<Storage.IStorage>();
        storage.GetEventStore(Arg.Any<EventStoreName>()).ReadModels.GetAll().Returns([readModel]);
        storage.GetEventStore(Arg.Any<EventStoreName>()).GetNamespace(Arg.Any<EventStoreNamespaceName>()).Sinks.GetFor(Arg.Any<ReadModelDefinition>()).Returns(sink);
        _silo.AddService(storage);

        _silo.AddProbe(_ => Substitute.For<INamespaces>());

        var client = new ConnectedClient { ConnectionId = "the-connection", Version = "1.0.0" };
        var connectedClients = Substitute.For<IConnectedClients>();
        connectedClients.GetConnectedClient(client.ConnectionId).Returns(client);
        _silo.AddProbe(_ => connectedClients);

        _observer = Substitute.For<IObserver>();
        _silo.AddProbe(_ => _observer);
        _recommendations = Substitute.For<IRecommendationsManager>();
        _silo.AddProbe(_ => _recommendations);

        _definition = new ReducerDefinition("the-reducer", EventSequenceId.Log, [], "the-read-model", true, []);
        var state = Substitute.For<IStorage<ReducerDefinition>>();
        state.State = _definition;
        _silo.Options.StorageFactory = _ => state;

        var key = new ConnectedObserverKey("the-reducer", "the-event-store", "the-namespace", EventSequenceId.Log, client.ConnectionId);
        _grain = await _silo.CreateGrainAsync<Reducer>(key.ToString());
    }
}
