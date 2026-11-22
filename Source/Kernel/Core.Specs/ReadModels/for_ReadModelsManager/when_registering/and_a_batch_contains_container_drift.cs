// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Concepts.Sinks;

namespace Cratis.Chronicle.ReadModels.for_ReadModelsManager.when_registering;

public class and_a_batch_contains_container_drift : given.a_read_models_manager
{
    static readonly ReadModelDefinition _first = DefinitionFor("first", "First");
    static readonly ReadModelDefinition _second = DefinitionFor("second", "Second");
    Exception _error;
    IEnumerable<ReadModelDefinition> _definitions;

    async Task Establish()
    {
        await _manager.Register([_first, _second]);
        _readModelGrain.ClearReceivedCalls();
        _projectionsManagerGrain.ClearReceivedCalls();
    }

    async Task Because()
    {
        _error = await Catch.Exception(() => _manager.Register(
        [
            _first with { DisplayName = "Changed", Sink = new(SinkConfigurationId.None, WellKnownSinkTypes.MongoDB) },
            DefinitionFor("new-model", "New model"),
            _second with { ContainerName = "another-container" }
        ]));
        _definitions = await _manager.GetDefinitions();
        await _manager.Register([_first, _second]);
    }

    [Fact] void should_refuse_the_batch() => _error.ShouldBeOfExactType<ReadModelContainerChangeRequiresExplicitUpdate>();
    [Fact] void should_keep_all_definitions() => _definitions.ShouldContainOnly(_first, _second);
    [Fact] void should_not_write_any_part_of_the_batch() => _silo.StorageManager.GetStorageStats(typeof(ReadModelsManager).FullName)!.Writes.ShouldEqual(1);
    [Fact] void should_not_propagate_or_invalidate_completed_definitions() => _readModelGrain.ReceivedCalls().ShouldBeEmpty();
    [Fact] void should_not_change_pending_evictions() => _projectionsManagerGrain.ReceivedCalls().ShouldBeEmpty();
    [Fact] void should_not_evict_pipelines() => _projectionPipelines.ReceivedCalls().ShouldBeEmpty();
    [Fact] void should_not_request_or_start_sink_change_replays() => _sinkChangeReplays.ReceivedCalls().ShouldBeEmpty();
}
