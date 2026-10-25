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

public class and_a_fixed_execution_context_is_supplied : a_generation_pipeline_manager
{
    static readonly JsonSerializerOptions _serializerOptions = new()
    {
        Converters = { new ConceptAsJsonConverterFactory() }
    };

    GenerationSinkContext _context;
    IProjectionPipeline _legacy;
    IProjectionPipeline _active;
    IProjectionPipeline _cached;
    IProjectionPipeline _refreshed;
    IProjectionPipeline _revised;

    void Establish()
    {
        _projection.EventSequenceId.Returns(EventSequenceId.Log);
        var execution = MaterializationExecutionSpecification.Capture(EventSequenceId.Log, JsonSerializer.Serialize(new { _projection.ReadModel }, _serializerOptions));
        var key = new MaterializationKey(new(Guid.NewGuid()), _namespace, _projection.Identifier, _projection.ReadModel.Sink.Type, SinkConfigurationId.None);
        var binding = new GenerationObserverBinding(key, MaterializationGenerationIdentity.For("projection", MaterializationGeneration.First, "Model"), EventSequenceId.Log, execution.Revision);
        _context = new(binding, execution, new(_store, binding, new(Guid.NewGuid()), 1));
        _storage.GetEventStore(_store).GetNamespace(_namespace).Sinks.GetFor(Arg.Any<GenerationSinkContext>()).Returns(_ => Substitute.For<ISink>());
    }

    async Task Because()
    {
        _legacy = await _manager.GetFor(_store, _namespace, _projection);
        _active = await _manager.GetFor(_store, _namespace, _projection, _context);
        _cached = await _manager.GetFor(_store, _namespace, _projection, _context);
        _refreshed = await _manager.GetFor(_store, _namespace, _projection, _context with { Grant = _context.Grant with { Epoch = 2 } });
        var execution = MaterializationExecutionSpecification.Capture(EventSequenceId.Log, _context.Execution.CanonicalContent.Replace("\"DisplayName\":\"Model\"", "\"DisplayName\":\"Different\"", StringComparison.Ordinal));
        var binding = _context.Binding with { Revision = execution.Revision };
        _revised = await _manager.GetFor(_store, _namespace, _projection, new GenerationSinkContext(binding, execution, _context.Grant with { Binding = binding }));
    }

    [Fact] void should_isolate_explicit_g1_from_the_legacy_pipeline() => _active.ShouldNotBeSame(_legacy);
    [Fact] void should_cache_an_identical_execution_admission() => _cached.ShouldBeSame(_active);
    [Fact] void should_not_reuse_a_pipeline_after_grant_refresh() => _refreshed.ShouldNotBeSame(_active);
    [Fact] void should_not_hide_execution_revision_behind_the_current_fingerprint() => _revised.ShouldNotBeSame(_active);
    [Fact] void should_share_the_legacy_handle_lock_for_generation_one() => HandleLock(_active).ShouldBeSame(HandleLock(_legacy));
    [Fact] void should_keep_the_handle_lock_across_grant_refresh() => HandleLock(_refreshed).ShouldBeSame(HandleLock(_active));
    [Fact] async Task should_route_the_explicit_context_to_storage() => await _storage.GetEventStore(_store).GetNamespace(_namespace).Sinks.Received().GetFor(_context);
}
