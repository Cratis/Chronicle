// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.ReadModels;

namespace Cratis.Chronicle.ReadModels.for_ReadModelsManager.when_registering;

public class and_an_existing_definitions_container_changed : given.a_read_models_manager
{
    static readonly ReadModelDefinition _first = DefinitionFor("some-read-model", "Display name");
    static readonly ReadModelDefinition _second = _first with { ContainerName = "a-different-container" };
    Exception _error;
    IEnumerable<ReadModelDefinition> _definitions;

    async Task Establish()
    {
        await _manager.Register([_first]);
        _readModelGrain.ClearReceivedCalls();
        _projectionsManagerGrain.ClearReceivedCalls();
    }

    async Task Because()
    {
        _error = await Catch.Exception(() => _manager.Register([_second]));
        _definitions = await _manager.GetDefinitions();
        await _manager.Register([_first]);
    }

    [Fact] void should_refuse_the_implicit_change() => _error.ShouldBeOfExactType<ReadModelContainerChangeRequiresExplicitUpdate>();
    [Fact] void should_identify_the_read_model() => _error.Message.ShouldContain(_first.Identifier.Value);
    [Fact] void should_name_the_stored_container() => _error.Message.ShouldContain(_first.ContainerName.Value);
    [Fact] void should_name_the_requested_container() => _error.Message.ShouldContain(_second.ContainerName.Value);
    [Fact] void should_explain_the_explicit_update_path() => _error.Message.ShouldContain("UpdateDefinition");
    [Fact] void should_keep_the_stored_definition() => _definitions.ShouldContainOnly(_first);
    [Fact] void should_not_write_state() => _silo.StorageManager.GetStorageStats(typeof(ReadModelsManager).FullName)!.Writes.ShouldEqual(1);
    [Fact] void should_not_propagate_or_invalidate_the_completed_definition() => _readModelGrain.ReceivedCalls().ShouldBeEmpty();
    [Fact] void should_not_find_pipelines_to_evict() => _projectionsManagerGrain.ReceivedCalls().ShouldBeEmpty();
    [Fact] void should_not_evict_pipelines() => _projectionPipelines.ReceivedCalls().ShouldBeEmpty();
    [Fact] void should_not_request_or_start_replays() => _sinkChangeReplays.ReceivedCalls().ShouldBeEmpty();
}
