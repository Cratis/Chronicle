// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Events.Constraints.for_ClosedStreamScope.when_checking_coverage;

public class and_closure_is_broader : Specification
{
    bool _covers;

    void Because() => _covers = new ClosedStreamScope(EventSourceId: "source").Covers(new(EventSourceId: "source", EventStreamType: EventStreamType.All, EventStreamId: EventStreamId.Default));

    [Fact] void should_cover_default_stream_for_the_source() => _covers.ShouldBeTrue();
}
