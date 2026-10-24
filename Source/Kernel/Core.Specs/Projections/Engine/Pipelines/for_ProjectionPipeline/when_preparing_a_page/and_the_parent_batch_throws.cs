// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Projections.Engine.Pipelines.for_ProjectionPipeline.when_preparing_a_page;

public class and_the_parent_batch_throws : given.a_page_pipeline
{
    string _result;

    void Establish()
    {
        _storage.GetInstancesOfAnyInRange(Arg.Is<IEnumerable<EventSourceId>>(sources => sources.Count() > 1), Arg.Any<IEnumerable<EventTypeId>>(), Arg.Any<EventSequenceNumber>(), Arg.Any<EventSequenceNumber>(), Arg.Any<CancellationToken>())
            .Returns<Task<IReadOnlyDictionary<EventSourceId, IReadOnlyList<AppendedEvent>>>>(_ => throw new InvalidOperationException("parent preparation failed"));
    }

    async Task Because()
    {
        await _pipeline.ResumeReplay(_replayContext);
        await _pipeline.PreparePage(_events);
        _result = await HandlePage(_pipeline);
    }

    [Fact] void should_use_normal_handling_after_the_prepare_failure() => _result.ShouldEqual("root-0:2,root-1:2");
    [Fact] void should_discard_all_partial_sink_preparation() => _sinkReads.ShouldEqual(4);
    [Fact] void should_discard_prepared_parent_entries() => _storage.Received(4).GetInstancesOfAnyInRange(Arg.Is<IEnumerable<EventSourceId>>(sources => sources.Count() == 1), Arg.Any<IEnumerable<EventTypeId>>(), Arg.Any<EventSequenceNumber>(), Arg.Any<EventSequenceNumber>(), Arg.Any<CancellationToken>());
}
