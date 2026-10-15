// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Concepts.Sinks;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.ReadModels;

namespace Cratis.Chronicle.ReadModels.for_ReadModelsManagerStorageProvider.when_writing_state;

public class and_only_some_definitions_were_modified : Specification
{
    ReadModelsManagerStorageProvider _provider;
    IReadModelDefinitionsStorage _definitionsStorage;
    ReadModelDefinition _unchanged;
    ReadModelDefinition _modified;
    IGrainState<ReadModelsManagerState> _state;

    void Establish()
    {
        var storage = Substitute.For<IStorage>();
        var eventStoreStorage = Substitute.For<IEventStoreStorage>();
        _definitionsStorage = Substitute.For<IReadModelDefinitionsStorage>();
        storage.GetEventStore("the-store").Returns(eventStoreStorage);
        eventStoreStorage.ReadModels.Returns(_definitionsStorage);
        _provider = new(storage);

        _unchanged = Create("unchanged");
        _modified = Create("modified");
        _state = new GrainState<ReadModelsManagerState> { State = new() { ReadModels = [_unchanged, _modified], Modified = [_modified] } };
    }

    Task Because() => _provider.WriteStateAsync("name", GrainId.Create("type", "the-store"), _state);

    [Fact] void should_save_the_modified_definition() => _definitionsStorage.Received(1).Save(_modified);
    [Fact] void should_not_save_the_unchanged_definition() => _definitionsStorage.DidNotReceive().Save(_unchanged);
    [Fact] void should_clear_the_modified_definitions() => _state.State.Modified.ShouldBeEmpty();

    static ReadModelDefinition Create(string identifier) => new(
        identifier,
        identifier,
        identifier,
        ReadModelOwner.None,
        ReadModelSource.Code,
        ReadModelObserverType.Projection,
        "some-projection",
        new SinkDefinition(SinkConfigurationId.None, WellKnownSinkTypes.MongoDB),
        new Dictionary<ReadModelGeneration, JsonSchema>(),
        []);
}
