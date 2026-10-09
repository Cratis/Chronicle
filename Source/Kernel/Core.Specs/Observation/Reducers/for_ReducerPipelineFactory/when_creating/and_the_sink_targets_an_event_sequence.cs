// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Observation.Reducers;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Concepts.Sinks;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.ReadModels;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.Sinks;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Observation.Reducers.for_ReducerPipelineFactory.when_creating;

public class and_the_sink_targets_an_event_sequence : Specification
{
    ReducerPipelineFactory _factory;
    ISinks _sinks;
    ReducerDefinition _definition;
    Exception _error;

    void Establish()
    {
        var grainFactory = Substitute.For<IGrainFactory>();
        var readModel = Substitute.For<IReadModel>();
        grainFactory.GetGrain<IReadModel>(Arg.Any<string>()).Returns(readModel);
        readModel.GetDefinition().Returns(new ReadModelDefinition(
            "target", "target", "Target", ReadModelOwner.None, ReadModelSource.Unknown, ReadModelObserverType.NotSet, ReadModelObserverIdentifier.Unspecified, new(SinkConfigurationId.None, WellKnownSinkTypes.EventSequence), new Dictionary<ReadModelGeneration, Schemas.JsonSchema>(), []));
        var storage = Substitute.For<IStorage>();
        var eventStoreStorage = Substitute.For<IEventStoreStorage>();
        var namespaceStorage = Substitute.For<IEventStoreNamespaceStorage>();
        storage.GetEventStore("store").Returns(eventStoreStorage);
        eventStoreStorage.GetNamespace("tenant").Returns(namespaceStorage);
        _sinks = Substitute.For<ISinks>();
        namespaceStorage.Sinks.Returns(_sinks);
        _factory = new(grainFactory, storage, Substitute.For<IObjectComparer>(), Substitute.For<IReadModelsCompliance>(), Options.Create(new ChronicleOptions()));
        _definition = new("reducer", Concepts.EventSequences.EventSequenceId.Log, [], "target", true, []);
    }

    async Task Because() => _error = await Catch.Exception(() => _factory.Create("store", "tenant", _definition));

    [Fact] void should_refuse_event_publication_explicitly() => _error.ShouldBeOfExactType<InconsistentEventSequenceSink>();
    [Fact] void should_not_create_a_read_model_sink() => _sinks.ReceivedCalls().ShouldBeEmpty();
}
