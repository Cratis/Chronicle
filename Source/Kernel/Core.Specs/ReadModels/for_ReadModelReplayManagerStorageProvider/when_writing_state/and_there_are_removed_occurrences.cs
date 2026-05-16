// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Concepts.Sinks;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.ReadModels;
using Cratis.Chronicle.Storage.Sinks;

namespace Cratis.Chronicle.ReadModels.for_ReadModelReplayManagerStorageProvider.when_writing_state;

public class and_there_are_removed_occurrences : Specification
{
    ReadModelReplayManagerStorageProvider _provider;
    IStorage _storage;
    IEventStoreStorage _eventStoreStorage;
    IEventStoreNamespaceStorage _eventStoreNamespaceStorage;
    IReadModelDefinitionsStorage _readModelDefinitionsStorage;
    IReplayedReadModelsStorage _replayedReadModelsStorage;
    ISinks _sinks;
    ISink _sink;
    ReadModelReplayManagerGrainKey _key;
    ReadModelOccurrence _removedOccurrence;
    IGrainState<ReadModelReplayManagerState> _state;

    void Establish()
    {
        _storage = Substitute.For<IStorage>();
        _eventStoreStorage = Substitute.For<IEventStoreStorage>();
        _eventStoreNamespaceStorage = Substitute.For<IEventStoreNamespaceStorage>();
        _readModelDefinitionsStorage = Substitute.For<IReadModelDefinitionsStorage>();
        _replayedReadModelsStorage = Substitute.For<IReplayedReadModelsStorage>();
        _sinks = Substitute.For<ISinks>();
        _sink = Substitute.For<ISink>();
        _provider = new(_storage);

        _key = new("TheEventStore", "TheNamespace", "TheReadModel");
        _removedOccurrence = new(
            (ObserverId)"TheObserver",
            DateTimeOffset.UtcNow,
            new(_key.ReadModel, ReadModelGeneration.First),
            "TheReadModel",
            "TheReadModel-20260516110000");
        _state = new GrainState<ReadModelReplayManagerState>
        {
            State = new()
            {
                RemovedOccurrences = [_removedOccurrence]
            }
        };

        var readModelDefinition = new ReadModelDefinition(
            _key.ReadModel,
            "TheReadModel",
            "The Read Model",
            ReadModelOwner.None,
            ReadModelSource.Code,
            ReadModelObserverType.Projection,
            "TheProjection",
            new SinkDefinition(SinkConfigurationId.None, WellKnownSinkTypes.MongoDB),
            new Dictionary<ReadModelGeneration, JsonSchema>
            {
                { ReadModelGeneration.First, new JsonSchema() }
            },
            []);

        _storage.GetEventStore(_key.EventStore).Returns(_eventStoreStorage);
        _eventStoreStorage.GetNamespace(_key.Namespace).Returns(_eventStoreNamespaceStorage);
        _eventStoreStorage.ReadModels.Returns(_readModelDefinitionsStorage);
        _eventStoreNamespaceStorage.ReplayedReadModels.Returns(_replayedReadModelsStorage);
        _eventStoreNamespaceStorage.Sinks.Returns(_sinks);
        _readModelDefinitionsStorage.Get(_key.ReadModel).Returns(readModelDefinition);
        _sinks.GetFor(readModelDefinition).Returns(_sink);
    }

    Task Because() => _provider.WriteStateAsync("name", GrainId.Create("type", (string)_key), _state);

    [Fact] void should_remove_replayed_occurrence() => _replayedReadModelsStorage.Received(1).Remove(_removedOccurrence);
    [Fact] void should_remove_replayed_read_model_container_from_sink() => _sink.Received(1).Remove(_removedOccurrence.RevertContainerName);
}
