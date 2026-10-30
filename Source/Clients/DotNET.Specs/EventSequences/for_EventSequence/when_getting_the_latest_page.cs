// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences.for_EventSequence;

public class when_getting_the_latest_page : given.a_paged_event_sequence
{
    async Task Because() => _page = await _eventSequence.GetLatest(1000, new(Tags: ["selected"]));

    [Fact] void should_start_at_the_tail_without_a_magic_sequence_number() => _request.From.ShouldBeNull();
    [Fact] void should_leave_the_stop_unbounded() => _request.Until.ShouldBeNull();
    [Fact] void should_request_newest_first_order() => _request.Backward.ShouldBeTrue();
    [Fact] void should_accept_the_maximum_count_without_clamping() => _request.Count.ShouldEqual(1000);
    [Fact] void should_forward_the_filter() => _request.Tags.ShouldEqual("selected");
    [Fact] void should_preserve_an_absent_continuation() => _page.Next.ShouldBeNull();
    [Fact] void should_expose_the_end_of_the_range() => _page.HasMore.ShouldBeFalse();
}
