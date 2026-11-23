// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Grpc.Core;

namespace Cratis.Chronicle.Projections.Engine.Pipelines.for_ProjectionPipeline;

public class when_the_database_becomes_marked : for_ProjectionPipelineManager.given.a_pipeline_manager_with_provenance
{
    readonly List<Exception?> _errors = [];

    async Task Because()
    {
        await _globalMarker.MarkQuarantined();
        await _namespaceMarker.MarkQuarantined();
        await Exercise(_binding.Pipeline);
        await Exercise(_generationPipeline);
        await Exercise(_isolated);
    }

    async Task Exercise(IProjectionPipeline pipeline)
    {
        _errors.Add(await Catch.Exception(() => pipeline.Handle(default!)));
        _errors.Add(await Catch.Exception(() => pipeline.ReadCurrent(default!)));
        _errors.Add(await Catch.Exception(() => pipeline.PreparePage([default!])));
        _errors.Add(await Catch.Exception(() => pipeline.BeginReplay(_replay)));
        _errors.Add(await Catch.Exception(() => pipeline.ResumeReplay(_replay)));
        _errors.Add(await Catch.Exception(() => pipeline.EndReplay(_replay)));
        _errors.Add(await Catch.Exception(pipeline.BeginBulk));
        _errors.Add(await Catch.Exception(pipeline.EndBulk));
    }

    [Fact] void should_recheck_every_retained_operation_including_page_prefetch() => _errors.TrueForAll(error => error is RpcException { StatusCode: StatusCode.PermissionDenied }).ShouldBeTrue();
    [Fact] void should_exercise_cached_generation_and_isolated_pipelines() => _errors.Count.ShouldEqual(24);
    [Fact] void should_not_touch_the_live_sink_after_denial() => _liveSink.ReceivedCalls().Any().ShouldBeFalse();
    [Fact] void should_not_touch_the_generation_sink_after_denial() => _generationSink.ReceivedCalls().Any().ShouldBeFalse();
    [Fact] void should_not_touch_the_isolated_sink_after_denial() => _isolatedSink.ReceivedCalls().Any().ShouldBeFalse();
    [Fact] void should_not_record_changesets() => _sharedChangesets.ReceivedCalls().Any().ShouldBeFalse();
}
