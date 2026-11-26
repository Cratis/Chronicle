// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.ReadModels.for_ReadModelsManager.when_registering;

public class and_a_batch_repeats_an_identifier_with_different_containers : given.a_read_models_manager
{
    Exception _error;

    async Task Because()
    {
        var definition = DefinitionFor("model", "Model");
        _error = await Catch.Exception(() => _manager.Register([definition, definition with { ContainerName = "another-container" }]));
    }

    [Fact] void should_refuse_the_batch() => _error.ShouldBeOfExactType<ReadModelContainerChangeRequiresExplicitUpdate>();
    [Fact] async Task should_not_register_either_definition() => (await _manager.GetDefinitions()).ShouldBeEmpty();
    [Fact] void should_not_write_state() => _silo.StorageManager.GetStorageStats(typeof(ReadModelsManager).FullName)!.Writes.ShouldEqual(0);
    [Fact] void should_not_propagate_to_read_models() => _readModelGrain.ReceivedCalls().ShouldBeEmpty();
    [Fact] void should_not_evict_pipelines() => _projectionPipelines.ReceivedCalls().ShouldBeEmpty();
    [Fact] void should_not_request_or_start_replays() => _sinkChangeReplays.ReceivedCalls().ShouldBeEmpty();
}
