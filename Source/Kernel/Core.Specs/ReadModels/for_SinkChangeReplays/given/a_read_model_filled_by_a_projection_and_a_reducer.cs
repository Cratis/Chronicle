// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation.Reducers;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Namespaces;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Projections;
using Cratis.Chronicle.Recommendations;
using Cratis.Chronicle.Storage;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.ReadModels.for_SinkChangeReplays.given;

public class a_read_model_filled_by_a_projection_and_a_reducer : Specification
{
    protected static readonly EventStoreName _eventStore = "store";
    protected static readonly ReadModelIdentifier _readModel = "orders";
    protected IGrainFactory _grainFactory;
    protected IObserver _observer;
    protected IRecommendationsManager _recommendations;
    protected SinkChangeReplays _replays;

    protected virtual DefinitionEvolutionPolicy Policy => DefinitionEvolutionPolicy.Automatic;

    void Establish()
    {
        _grainFactory = Substitute.For<IGrainFactory>();
        _observer = Substitute.For<IObserver>();
        _recommendations = Substitute.For<IRecommendationsManager>();
        _grainFactory.GetGrain<IObserver>(Arg.Any<string>(), Arg.Any<string>()).Returns(_observer);
        _grainFactory.GetGrain<IRecommendationsManager>(Arg.Any<long>(), Arg.Any<string>(), Arg.Any<string>()).Returns(_recommendations);
        var namespaces = Substitute.For<INamespaces>();
        namespaces.GetAll().Returns([(EventStoreNamespaceName)"default"]);
        _grainFactory.GetGrain<INamespaces>(Arg.Any<string>(), Arg.Any<string>()).Returns(namespaces);
        var projections = Substitute.For<IProjectionsManager>();
        projections.GetProjectionDefinitions().Returns([Projection("orders-projection", _readModel), Projection("other-projection", "other")]);
        _grainFactory.GetGrain<IProjectionsManager>(Arg.Any<string>(), Arg.Any<string>()).Returns(projections);

        var storage = Substitute.For<IStorage>();
        var store = Substitute.For<IEventStoreStorage>();
        storage.GetEventStore(_eventStore).Returns(store);
        store.Reducers.GetAll().Returns([new ReducerDefinition("orders-reducer", EventSequenceId.Log, [], _readModel, true, [])]);

        _replays = new(_grainFactory, storage, Options.Create(new ChronicleOptions { Observers = new() { DefinitionEvolution = Policy } }), NullLogger<SinkChangeReplays>.Instance);
    }

    static ProjectionDefinition Projection(ProjectionId identifier, ReadModelIdentifier readModel) => new(
        ProjectionOwner.Client,
        EventSequenceId.Log,
        identifier,
        readModel,
        true,
        true,
        new System.Text.Json.Nodes.JsonObject(),
        new Dictionary<Concepts.Events.EventType, FromDefinition>(),
        new Dictionary<Concepts.Events.EventType, JoinDefinition>(),
        new Dictionary<Properties.PropertyPath, ChildrenDefinition>(),
        [],
        new FromEveryDefinition(new Dictionary<Properties.PropertyPath, string>(), false),
        new Dictionary<Concepts.Events.EventType, RemovedWithDefinition>(),
        new Dictionary<Concepts.Events.EventType, RemovedWithJoinDefinition>());
}
