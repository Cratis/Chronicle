// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Graphs;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Sinks;
using Cratis.Chronicle.Projections.Engine.Graphs;
using Cratis.Chronicle.ReadModels;
using Cratis.Chronicle.Storage.Graphs;
using Grpc.Core;

namespace Cratis.Chronicle.Projections.Engine.Pipelines.for_ProjectionPipelineManager;

public class when_a_global_graph_targets_a_marked_namespace : given.a_materializing_pipeline_manager
{
    IReadModelSinkProvenance _provenance;
    IProjection _graph;
    Exception? _failure;

    void Establish()
    {
        _graph = Substitute.For<IProjection>();
        _readModel = _readModel with { Sink = new(SinkConfigurationId.None, WellKnownSinkTypes.Neo4j) };
        _graph.ReadModel.Returns(_readModel);
        _graph.Scope.Returns(ProjectionScope.Global);
        _graph.EventSequenceId.Returns(EventSequenceId.Log);
        _graph.Graph.Returns(new GraphProjectionPlan(new GraphDefinition("graph", [], []), new Dictionary<EventType, GraphEventMappings>()));
        _namespaceStorage.Sinks.GetGraphFor(_readModel).Returns(Substitute.For<IGraphSink>());
        _provenance = Substitute.For<IReadModelSinkProvenance>();
        _provenance.EnsureReadable(_store, _namespace, _readModel)
            .Returns(Task.FromException(new RpcException(new Status(StatusCode.PermissionDenied, string.Empty))));
        _manager = CreateManager(_provenance);
        _namespaceStorage.Sinks.ClearReceivedCalls();
    }

    async Task Because() => _failure = await Catch.Exception(() => _manager.GetFor(_store, _namespace, _graph));

    [Fact] void should_refuse_the_marked_namespace() => ((RpcException)_failure!).StatusCode.ShouldEqual(StatusCode.PermissionDenied);
    [Fact] async Task should_not_use_the_global_database() => await _provenance.DidNotReceive().EnsureReadable(_store, EventStoreNamespaceName.NotSet, _readModel);
    [Fact] async Task should_not_open_the_graph_before_admission() => await _namespaceStorage.Sinks.DidNotReceive().GetGraphFor(_readModel);
}
