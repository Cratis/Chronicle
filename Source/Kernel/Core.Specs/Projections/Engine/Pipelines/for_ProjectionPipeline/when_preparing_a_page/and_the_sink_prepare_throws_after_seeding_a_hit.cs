// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;

namespace Cratis.Chronicle.Projections.Engine.Pipelines.for_ProjectionPipeline.when_preparing_a_page;

public class and_the_sink_prepare_throws_after_seeding_a_hit : given.a_page_pipeline
{
    string _result;

    void Establish()
    {
        _prefetch.PrefetchPage(Arg.Any<IEnumerable<Key>>()).Returns(_ =>
        {
            _prepared["root-0"] = _documents["root-0"];
            throw new InvalidOperationException("prepare failed after a partial read");
        });
    }

    async Task Because()
    {
        await _pipeline.ResumeReplay(_replayContext);
        await _pipeline.PreparePage(_events);
        _result = await HandlePage(_pipeline);
    }

    [Fact] void should_use_normal_handling_after_the_prepare_failure() => _result.ShouldEqual("root-0:2,root-1:2");
    [Fact] void should_discard_all_partial_sink_preparation() => _sinkReads.ShouldEqual(4);
    [Fact] void should_resolve_each_child_parent_from_the_event_log() => _storage.Received(4).GetInstancesOfAnyInRange(Arg.Any<IEnumerable<EventSourceId>>(), Arg.Any<IEnumerable<EventTypeId>>(), Arg.Any<EventSequenceNumber>(), Arg.Any<EventSequenceNumber>(), Arg.Any<CancellationToken>());
}
