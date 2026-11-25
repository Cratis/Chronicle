// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Concepts.Sinks;
using Cratis.Chronicle.ReadModels;
using Cratis.Chronicle.Storage.InMemory.Sinks;
using Cratis.Chronicle.Storage.Sinks;

namespace Cratis.Chronicle.Projections.Engine.Pipelines.for_ProjectionPipelineManager.given;

public class a_pipeline_manager_with_provenance : a_materializing_pipeline_manager
{
    protected InMemorySinkProvenanceStorage _globalMarker;
    protected InMemorySinkProvenanceStorage _namespaceMarker;
    protected IProjectionPipeline _cached;
    protected IProjection _generationProjection;
    protected IProjectionPipeline _generationPipeline;
    protected IProjectionPipeline _isolated;
    protected ISink _isolatedSink;
    protected ProjectionPipelineBinding _binding;

    async Task Establish()
    {
        _readModel = _readModel with { Sink = new(SinkConfigurationId.None, WellKnownSinkTypes.InMemory) };
        _definition = _definition with { Scope = ProjectionScope.Global };
        _projection = await _factory.Create(_store, _namespace, _definition, _readModel, _schemas);
        _globalMarker = new();
        _namespaceMarker = new();
        var globalStorage = _managerStorage.GetEventStore(_store).GetNamespace(EventStoreNamespaceName.NotSet);
        globalStorage.Sinks.GetProvenance(Arg.Any<ReadModelDefinition>()).Returns(_globalMarker);
        globalStorage.Sinks.GetFor(_readModel).Returns(_liveSink);
        _namespaceStorage.Sinks.GetProvenance(Arg.Any<ReadModelDefinition>()).Returns(_namespaceMarker);
        _namespaceStorage.Sinks.GetFor(_readModel, _generation.Destination).Returns(_generationSink);
        _manager = CreateManager(new ReadModelSinkProvenance(_managerStorage));
        _cached = await _manager.GetFor(_store, _namespace, _projection);
        _generationProjection = await _factory.Create(_store, _namespace, _definition with { Scope = ProjectionScope.Namespaced }, _readModel, _schemas);
        _generationPipeline = await _manager.GetFor(_store, _namespace, _generationProjection, _generation);
        _isolatedSink = Substitute.For<ISink>();
        _isolated = _manager.CreateIsolated(_store, _namespace, _projection, _isolatedSink);
        _manager.TryGetFor(_store, _namespace, _projection.Identifier, out var binding);
        _binding = binding!;
        ClearSharedCalls();
        _generationSink.ClearReceivedCalls();
        _isolatedSink.ClearReceivedCalls();
    }
}
