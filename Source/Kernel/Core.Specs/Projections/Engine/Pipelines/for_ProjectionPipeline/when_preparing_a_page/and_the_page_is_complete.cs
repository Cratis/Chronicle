// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Projections.Engine.Pipelines.for_ProjectionPipeline.when_preparing_a_page;

public class and_the_page_is_complete : given.a_page_pipeline
{
    async Task Because()
    {
        await _pipeline.ResumeReplay(_replayContext);
        await _pipeline.PreparePage(_events);
        _pipeline.ClearPreparedPage();
        await HandlePage(_pipeline);
    }

    [Fact] void should_clear_the_sink_preparation() => _sinkReads.ShouldEqual(4);
    [Fact] void should_resolve_each_child_parent_from_the_event_log() => _storage.Received(4).GetInstancesOfAnyInRange(Arg.Any<IEnumerable<EventSourceId>>(), Arg.Any<IEnumerable<EventTypeId>>(), Arg.Any<EventSequenceNumber>(), Arg.Any<EventSequenceNumber>(), Arg.Any<CancellationToken>());
}
