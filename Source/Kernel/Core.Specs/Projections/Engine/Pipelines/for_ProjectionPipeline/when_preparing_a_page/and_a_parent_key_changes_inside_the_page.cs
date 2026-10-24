// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Projections.Engine.Pipelines.for_ProjectionPipeline.when_preparing_a_page;

public class and_a_parent_key_changes_inside_the_page : given.a_page_pipeline
{
    string _baseline;
    string _preparedResult;

    void Establish()
    {
        _events = [EventFor("child-0", _childType, 10, "parent-0"), EventFor("child-1", _childType, 11, "parent-1"),
            EventFor("child-2", _childType, 13, "parent-0"), EventFor("child-3", _childType, 14, "parent-1")];
        _additionalParents.Add(EventFor("parent-0", _parentType, 12, "root-1"));
        _additionalParents.Add(EventFor("parent-0", _parentType, 20, "future-root"));
    }

    async Task Because()
    {
        await _pipeline.ResumeReplay(_replayContext);
        _baseline = await HandlePage(_pipeline);
        await _pipeline.EndBulk();
        ResetCounts();
        await _pipeline.ResumeReplay(_replayContext);
        await _pipeline.PreparePage(_events);
        _preparedResult = await HandlePage(_pipeline);
    }

    [Fact] void should_resolve_each_child_at_its_own_sequence() => _preparedResult.ShouldEqual("root-0:1,root-1:3");
    [Fact] void should_match_unprepared_handling() => _preparedResult.ShouldEqual(_baseline);
    [Fact] void should_read_one_bounded_parent_batch() => _storage.Received(1).GetInstancesOfAnyInRange(
        Arg.Is<IEnumerable<EventSourceId>>(sources => sources.Count() == 2), Arg.Any<IEnumerable<EventTypeId>>(), 10, 14, Arg.Any<CancellationToken>());
    [Fact] void should_not_read_per_child() => _storage.DidNotReceive().GetInstancesOfAnyInRange(
        Arg.Is<IEnumerable<EventSourceId>>(sources => sources.Count() == 1), Arg.Any<IEnumerable<EventTypeId>>(), Arg.Any<EventSequenceNumber>(), Arg.Any<EventSequenceNumber>(), Arg.Any<CancellationToken>());
    [Fact] void should_prepare_the_sink_once() => _sinkBatches.ShouldEqual(1);
    [Fact] void should_use_only_prepared_sink_documents() => _sinkReads.ShouldEqual(0);
}
