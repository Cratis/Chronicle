// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.ReadModels;

namespace Cratis.Chronicle.ReadModels.for_ReadModelsManager.when_registering;

public class and_persisted_container_metadata_differs : given.a_read_models_manager
{
    static readonly ReadModelDefinition _persisted = DefinitionFor("model", "Model");
    Exception _error;

    void Establish() => _silo.StorageManager.GetStorage<ReadModelsManagerState>(typeof(ReadModelsManager).FullName).State.ReadModels = [_persisted];

    async Task Because() => _error = await Catch.Exception(() => _manager.RegisterSingle(_persisted with { ContainerName = "another-container" }));

    [Fact] void should_refuse_drift_after_activation_without_completed_cache_entries() => _error.ShouldBeOfExactType<ReadModelContainerChangeRequiresExplicitUpdate>();
    [Fact] async Task should_keep_the_persisted_definition() => (await _manager.GetDefinitions()).ShouldContainOnly(_persisted);
    [Fact] void should_not_write_state() => _silo.StorageManager.GetStorageStats(typeof(ReadModelsManager).FullName)!.Writes.ShouldEqual(0);
    [Fact] void should_not_propagate_to_read_models() => _readModelGrain.ReceivedCalls().ShouldBeEmpty();
    [Fact] void should_not_evict_pipelines() => _projectionPipelines.ReceivedCalls().ShouldBeEmpty();
    [Fact] void should_not_request_or_start_replays() => _sinkChangeReplays.ReceivedCalls().ShouldBeEmpty();
}
