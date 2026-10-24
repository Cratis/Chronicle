// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Projections.Engine.Pipelines.for_ProjectionPipeline.when_preparing_a_page;

public class and_the_pipeline_is_replaced_mid_page : given.a_page_pipeline
{
    string _result;

    async Task Because()
    {
        await _pipeline.ResumeReplay(_replayContext);
        await _pipeline.PreparePage(_events);
        _pipeline.ClearPreparedPage();
        var replacement = CreatePipeline();
        await replacement.ResumeReplay(_replayContext);
        _result = await HandlePage(replacement);
    }

    [Fact] void should_produce_the_same_result_without_the_old_plan() => _result.ShouldEqual("root-0:2,root-1:2");
    [Fact] void should_not_reuse_the_old_sink_preparation() => _sinkReads.ShouldEqual(4);
    [Fact] void should_resolve_parent_reads_in_the_replacement() => _storage.Received(2).TryGetLastInstanceOfAny(Arg.Any<EventSourceId>(), Arg.Any<IEnumerable<EventTypeId>>());
}
