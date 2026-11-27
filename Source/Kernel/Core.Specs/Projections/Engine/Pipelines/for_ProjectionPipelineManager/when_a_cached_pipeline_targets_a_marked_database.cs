// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Grpc.Core;

namespace Cratis.Chronicle.Projections.Engine.Pipelines.for_ProjectionPipelineManager;

public class when_a_cached_pipeline_targets_a_marked_database : given.a_pipeline_manager_with_provenance
{
    Exception? _ordinaryError;
    Exception? _generationError;
    IProjectionPipeline _independentGeneration;

    async Task Because()
    {
        await _globalMarker.MarkQuarantined();
        _ordinaryError = await Catch.Exception(() => _manager.GetFor(_store, _namespace, _projection));
        _independentGeneration = await _manager.GetFor(_store, _namespace, _generationProjection, _generation);
        await _namespaceMarker.MarkQuarantined();
        _generationError = await Catch.Exception(() => _manager.GetFor(_store, _namespace, _generationProjection, _generation));
    }

    [Fact] void should_recheck_the_global_database_before_returning_a_cached_live_pipeline() => ((RpcException)_ordinaryError!).StatusCode.ShouldEqual(StatusCode.PermissionDenied);
    [Fact] void should_route_a_generation_to_its_explicit_namespace() => _independentGeneration.ShouldEqual(_generationPipeline);
    [Fact] void should_recheck_the_generation_database_before_a_cache_hit() => ((RpcException)_generationError!).StatusCode.ShouldEqual(StatusCode.PermissionDenied);
    [Fact] void should_not_read_or_reprovision_the_live_sink() => _liveSink.ReceivedCalls().Any().ShouldBeFalse();
    [Fact] void should_not_read_or_reprovision_the_generation_sink() => _generationSink.ReceivedCalls().Any().ShouldBeFalse();
}
