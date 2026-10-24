// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Monads;

namespace Cratis.Chronicle.Projections.Engine.Pipelines.for_ProjectionPipeline.when_preparing_a_page;

public class and_the_parent_batch_is_partial : given.a_page_pipeline
{
    string _result;

    void Establish()
    {
        _storage.GetLastInstancesOfAny(Arg.Any<IEnumerable<EventSourceId>>(), Arg.Any<IEnumerable<EventTypeId>>())
            .Returns(Task.FromResult<IReadOnlyDictionary<EventSourceId, Option<AppendedEvent>>>(new Dictionary<EventSourceId, Option<AppendedEvent>> { [new("parent-0")] = new(_parents[new("parent-0")]) }));
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
