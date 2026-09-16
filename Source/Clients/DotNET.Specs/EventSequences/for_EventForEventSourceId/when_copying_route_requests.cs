// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.EventSequences.for_EventForEventSourceId;

public class when_copying_route_requests : Specification
{
    EventForEventSourceId _implicit;
    EventForEventSourceId _explicit;
    EventForEventSourceId _implicitCopy;
    EventForEventSourceId _explicitCopy;

    void Establish()
    {
        _implicit = new("source", new object());
        _explicit = _implicit with
        {
            EventSourceType = EventSourceType.Default,
            EventStreamType = EventStreamType.All,
            EventStreamId = EventStreamId.Default
        };
    }

    void Because()
    {
        _implicitCopy = _implicit with { Subject = "person" };
        _explicitCopy = _explicit with { Subject = "person" };
    }

    [Fact] void should_preserve_omitted_source_type() => _implicitCopy.RequestedEventSourceType.ShouldBeNull();
    [Fact] void should_preserve_omitted_stream_type() => _implicitCopy.RequestedEventStreamType.ShouldBeNull();
    [Fact] void should_preserve_omitted_stream_id() => _implicitCopy.RequestedEventStreamId.ShouldBeNull();
    [Fact] void should_preserve_explicit_source_type() => _explicitCopy.RequestedEventSourceType.ShouldEqual(EventSourceType.Default);
    [Fact] void should_preserve_explicit_stream_type() => _explicitCopy.RequestedEventStreamType.ShouldEqual(EventStreamType.All);
    [Fact] void should_preserve_explicit_stream_id() => _explicitCopy.RequestedEventStreamId.ShouldEqual((EventStreamId)EventStreamId.Default);
    [Fact] void should_preserve_public_value_equality() => _implicitCopy.ShouldEqual(_explicitCopy);
}
