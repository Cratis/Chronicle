// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Projections.Generations;
using Cratis.Chronicle.Concepts.Projections.Generations.Bindings;
using Cratis.Chronicle.Concepts.Sinks;
using Cratis.Chronicle.Storage.Sinks;
using Cratis.Json;

namespace Cratis.Chronicle.Projections.Engine.Pipelines.for_ProjectionPipelineManager.when_getting_for_generation;

public class and_a_newer_grant_is_admitted : a_generation_pipeline_manager
{
    static readonly JsonSerializerOptions _serializerOptions = new()
    {
        Converters = { new ConceptAsJsonConverterFactory() }
    };

    GenerationSinkContext _older;
    GenerationSinkContext _newer;
    IProjectionPipeline _first;
    IProjectionPipeline _afterNewer;
    IProjectionPipeline _newerAgain;
    IProjectionPipeline _olderAgain;

    void Establish()
    {
        _projection.EventSequenceId.Returns(EventSequenceId.Log);
        var execution = MaterializationExecutionSpecification.Capture(EventSequenceId.Log, JsonSerializer.Serialize(new { _projection.ReadModel }, _serializerOptions));
        var key = new MaterializationKey(new(Guid.NewGuid()), _namespace, _projection.Identifier, _projection.ReadModel.Sink.Type, SinkConfigurationId.None);
        var binding = new GenerationObserverBinding(key, _second, EventSequenceId.Log, execution.Revision);
        _older = new(binding, execution, new(_store, binding, new(Guid.NewGuid()), 1));
        _newer = _older with { Grant = _older.Grant with { Epoch = 2 } };
        _storage.GetEventStore(_store).GetNamespace(_namespace).Sinks.GetFor(Arg.Any<GenerationSinkContext>()).Returns(_ => Substitute.For<ISink>());
    }

    async Task Because()
    {
        _first = await _manager.GetFor(_store, _namespace, _projection, _older);
        _afterNewer = await _manager.GetFor(_store, _namespace, _projection, _newer);
        _newerAgain = await _manager.GetFor(_store, _namespace, _projection, _newer);
        _olderAgain = await _manager.GetFor(_store, _namespace, _projection, _older);
    }

    [Fact] void should_build_a_new_pipeline_for_the_newer_grant() => _afterNewer.ShouldNotBeSame(_first);
    [Fact] void should_keep_serving_the_newer_grant_from_the_cache() => _newerAgain.ShouldBeSame(_afterNewer);
    [Fact] void should_have_dropped_the_superseded_cache_entry() => _olderAgain.ShouldNotBeSame(_first);
}
