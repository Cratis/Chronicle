// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Events.for_EventContext;

public class when_creating_without_an_occurrence_time : Specification
{
    EventContext _context;

    void Because() => _context = EventContext.From(
        EventStoreName.NotSet,
        EventStoreNamespaceName.NotSet,
        EventType.Unknown,
        EventSourceType.Default,
        EventSourceId.New(),
        EventStreamType.All,
        EventStreamId.Default,
        EventSequenceNumber.Unavailable,
        CorrelationId.NotSet);

    [Fact] void should_stamp_occurred_in_utc() => _context.Occurred.Offset.ShouldEqual(TimeSpan.Zero);
}
