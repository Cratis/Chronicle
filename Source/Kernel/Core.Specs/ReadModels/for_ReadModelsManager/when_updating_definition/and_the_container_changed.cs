// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.ReadModels;

namespace Cratis.Chronicle.ReadModels.for_ReadModelsManager.when_updating_definition;

public class and_the_container_changed : given.a_read_models_manager
{
    static readonly ReadModelDefinition _first = DefinitionFor("some-read-model", "Display name");
    static readonly ReadModelDefinition _second = _first with { ContainerName = "a-different-container" };
    IEnumerable<ReadModelDefinition> _definitions;

    async Task Establish()
    {
        await _manager.RegisterSingle(_first);
        _readModelGrain.ClearReceivedCalls();
        _namespacesGrain.GetAll().Returns(["first", "second"]);
        _projectionsManagerGrain.GetProjectionDefinitions().Returns([ProjectionTargeting("projection", _first.Identifier)]);
    }

    async Task Because()
    {
        await _manager.UpdateDefinition(_second);
        _definitions = await _manager.GetDefinitions();
        await _manager.RegisterSingle(_second);
    }

    [Fact] void should_store_the_new_container() => _definitions.ShouldContainOnly(_second);
    [Fact] void should_persist_the_explicit_update_once() => _silo.StorageManager.GetStorageStats(typeof(ReadModelsManager).FullName)!.Writes.ShouldEqual(2);
    [Fact] async Task should_propagate_the_definition_once() => await _readModelGrain.Received(1).SetDefinition(_second);
    [Fact] void should_evict_the_first_namespaces_pipeline() => _projectionPipelines.Received(1).EvictFor(_eventStore, "first", "projection");
    [Fact] void should_evict_the_second_namespaces_pipeline() => _projectionPipelines.Received(1).EvictFor(_eventStore, "second", "projection");
    [Fact] void should_not_request_a_sink_change_replay() => _sinkChangeReplays.ReceivedCalls().ShouldBeEmpty();
}
