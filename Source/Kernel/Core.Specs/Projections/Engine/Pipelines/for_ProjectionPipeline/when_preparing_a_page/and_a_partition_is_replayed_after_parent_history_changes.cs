// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Engine.Pipelines.for_ProjectionPipeline.when_preparing_a_page;

public class and_a_partition_is_replayed_after_parent_history_changes : given.a_page_pipeline
{
    string _result;

    async Task Because()
    {
        await _pipeline.ResumeReplay(_replayContext);
        await _pipeline.PreparePage(_events);
        await HandlePage(_pipeline);
        await _pipeline.EndBulk();
        ResetCounts();
        _parents[new("parent-0")] = EventFor("parent-0", _parentType, 1, "root-1");
        await _pipeline.ResumeReplay(_replayContext);
        await _pipeline.PreparePage(_events);
        _result = await HandlePage(_pipeline);
    }

    [Fact] void should_use_current_history_after_the_partition_rewinds() => _result.ShouldEqual("root-0:0,root-1:4");
    [Fact] void should_read_the_replayed_page_in_one_sink_batch() => _sinkBatches.ShouldEqual(1);
}
