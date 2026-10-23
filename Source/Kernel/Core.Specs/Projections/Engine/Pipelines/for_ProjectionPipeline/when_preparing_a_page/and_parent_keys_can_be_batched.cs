// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Projections.Engine.Pipelines.for_ProjectionPipeline.when_preparing_a_page;

public class and_parent_keys_can_be_batched : given.a_page_pipeline
{
    string _baseline;
    string _preparedResult;
    int _preparedSinkReads;

    async Task Because()
    {
        await _pipeline.ResumeReplay(_replayContext);
        _baseline = await HandlePage(_pipeline);
        await _pipeline.EndBulk();
        ResetCounts();
        await _pipeline.ResumeReplay(_replayContext);
        await _pipeline.PreparePage(_events);
        _preparedResult = await HandlePage(_pipeline);
        _preparedSinkReads = _sinkReads;
    }

    [Fact] void should_produce_identical_results() => _preparedResult.ShouldEqual(_baseline);
    [Fact] void should_batch_the_sink_reads_once() => _sinkBatches.ShouldEqual(1);
    [Fact] void should_not_read_sink_documents_individually() => _preparedSinkReads.ShouldEqual(0);
    [Fact] void should_batch_the_parent_reads_once() => _storage.Received(1).GetLastInstancesOfAny(Arg.Any<IEnumerable<EventSourceId>>(), Arg.Any<IEnumerable<EventTypeId>>());
    [Fact] void should_not_read_parent_events_individually() => _storage.DidNotReceive().TryGetLastInstanceOfAny(Arg.Any<EventSourceId>(), Arg.Any<IEnumerable<EventTypeId>>());
}
