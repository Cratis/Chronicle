// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.EventTypes;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.ReadModels;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage.Sinks;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using FailedPartition = Cratis.Chronicle.Storage.Sinks.FailedPartition;

namespace Cratis.Chronicle.Projections.Engine.Pipelines.for_ProjectionPipelineOwnership.given;

public class an_evicted_graph_with_a_live_pipeline : for_ProjectionsManager.given.a_manager_with_shared_definitions
{
    protected readonly FailedPartition _failure = new("failed-partition", 42UL);
    protected readonly CancellationToken _deadline = new(true);
    protected IProjection _ownedGraph;
    protected IProjectionPipeline _livePipeline;
    protected ProjectionPipelineManager _pipelines;
    protected ISink _sink;
    protected ObserverDetails _details;
    protected ProjectionCatchupHandler _catchup;
    protected ProjectionReplayHandler _replay;
    int _compilationsBefore;

    async Task Establish()
    {
        _projectionFactory.Create(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>(), Arg.Any<ProjectionDefinition>(), Arg.Any<ReadModelDefinition>(), Arg.Any<IEnumerable<EventTypeSchema>>()).Returns(call =>
        {
            var definition = call.Arg<ProjectionDefinition>();
            var graph = Substitute.For<IProjection>();
            graph.Identifier.Returns(definition.Identifier);
            graph.EventSequenceId.Returns(definition.EventSequenceId);
            graph.ReadModel.Returns(call.Arg<ReadModelDefinition>());
            graph.DefinitionFingerprint.Returns(ProjectionFactory.DefinitionFingerprintFor(definition));
            return graph;
        });
        _details = new(new(_firstDefinition.Identifier.Value, _eventStore, _namespace, _firstDefinition.EventSequenceId), ObserverType.Projection);
        _sink = Substitute.For<ISink>();
        _sink.FlushBulk().Returns(new BulkFlushResult(null, [_failure]));
        _sink.EndBulk().Returns(Task.FromResult<IEnumerable<FailedPartition>>([_failure]));
        _sink.EndBulk(_deadline).Returns(Task.FromResult<IEnumerable<FailedPartition>>([_failure]));
        _storage.GetEventStore(_eventStore).GetNamespace(_namespace).Sinks.GetFor(_firstReadModelDefinition).Returns(_sink);
        _pipelines = CreatePipelineManager();
        _ownedGraph = (await _manager.GetCurrent(_eventStore, _namespace, _firstDefinition.Identifier))!;
        _livePipeline = await _pipelines.GetFor(_eventStore, _namespace, _ownedGraph);
        for (var tenant = 0; tenant < ProjectionsManager.CacheCapacity; tenant++)
        {
            await _manager.GetCurrent(_eventStore, $"overflow-{tenant}", _firstDefinition.Identifier);
        }

        _compilationsBefore = _projectionFactory.ReceivedCalls().Count();
        _catchup = new(_manager, _pipelines, _grainFactory, NullLogger<ProjectionCatchupHandler>.Instance);
        _replay = new(_manager, _grainFactory, _storage, _pipelines, Options.Create(new ChronicleOptions()), NullLogger<ProjectionReplayHandler>.Instance);
    }

    protected void GraphShouldRemainEvicted() => _manager.TryGet(_eventStore, _namespace, _firstDefinition.Identifier, out _).ShouldBeFalse();
    protected void ShouldNotCompileAnotherGraph() => _projectionFactory.ReceivedCalls().Count().ShouldEqual(_compilationsBefore);

    ProjectionPipelineManager CreatePipelineManager() => new(
        _storage,
        _grainFactory,
        Substitute.For<IObjectComparer>(),
        Substitute.For<ITypeFormats>(),
        Substitute.For<IReadModelsCompliance>(),
        Options.Create(new ChronicleOptions()),
        NullLoggerFactory.Instance);
}
