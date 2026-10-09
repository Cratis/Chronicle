// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Events.Constraints.for_ClosedStreamScope;

public class when_checking_default_stream_only : Specification
{
    bool[] _results;

    void Because() => _results = new ClosedStreamScope[]
    {
        new(EventStreamType: EventStreamType.All),
        new(EventStreamId: EventStreamId.Default),
        new(EventStreamType: EventStreamType.All, EventStreamId: EventStreamId.Default),
        new(EventSourceId: "source", EventStreamType: EventStreamType.All, EventStreamId: EventStreamId.Default),
        new(EventSourceType: EventSourceType.Default),
        new(EventStreamType: "transactions"),
        new()
    }.Select(scope => scope.IsDefaultStreamOnly).ToArray();

    [Fact] void should_refuse_only_default_stream_dimensions_without_a_source() => _results.ShouldEqual([true, true, true, false, false, false, false]);
}
