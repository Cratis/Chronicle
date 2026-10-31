// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.EventSequences.for_EventSequence;

public class when_getting_a_page : given.a_paged_event_sequence
{
    readonly CancellationTokenSource _cancellation = new();

    void Establish() => _response.Next = 43;

    async Task Because()
    {
        var filter = new EventSequenceFilter("source", "source-type", "stream-type", "stream", [new EventType("first", EventTypeGeneration.First), new EventType("second", EventTypeGeneration.First)], ["red", "blue"]);
        var request = new EventSequencePageRequest(20, 10, EventSequenceReadDirection.Forward, 100, filter);
        _page = await _eventSequence.GetPage(request, _cancellation.Token);
    }

    [Fact] void should_forward_the_inclusive_start() => _request.From.ShouldEqual((ulong?)20);
    [Fact] void should_forward_the_inclusive_stop() => _request.Until.ShouldEqual((ulong?)100);
    [Fact] void should_forward_the_count() => _request.Count.ShouldEqual(10);
    [Fact] void should_request_forward_order() => _request.Backward.ShouldBeFalse();
    [Fact] void should_forward_the_source_identifier() => _request.EventSourceId.ShouldEqual("source");
    [Fact] void should_forward_the_source_type() => _request.EventSourceType.ShouldEqual("source-type");
    [Fact] void should_forward_the_stream_type() => _request.EventStreamType.ShouldEqual("stream-type");
    [Fact] void should_forward_the_stream_identifier() => _request.EventStreamId.ShouldEqual("stream");
    [Fact] void should_forward_event_type_ids() => _request.EventTypeIds.ShouldEqual("first,second");
    [Fact] void should_forward_tags() => _request.Tags.ShouldEqual("red,blue");
    [Fact] void should_forward_cancellation() => _callContext.CancellationToken.ShouldEqual(_cancellation.Token);
    [Fact] void should_convert_the_continuation() => _page.Next.ShouldEqual((EventSequenceNumber)43UL);
    [Fact] void should_expose_more_matching_events() => _page.HasMore.ShouldBeTrue();

    void Destroy() => _cancellation.Dispose();
}
