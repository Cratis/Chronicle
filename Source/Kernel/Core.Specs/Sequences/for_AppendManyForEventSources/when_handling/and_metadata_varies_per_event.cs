// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Sequences.for_AppendManyForEventSources.when_handling;

public class and_metadata_varies_per_event : Sequences.given.an_append_endpoint
{
    async Task Because() => await new AppendManyForEventSources(
        "store",
        "namespace",
        "event-log",
        [
            new("first", string.Empty, string.Empty, string.Empty, new EventType("event", 1, false), "{}"),
            new("second", "Account", "Payments", "September", new EventType("event", 1, false), "{}"),
            new("third", null!, null!, null!, new EventType("event", 1, false), "{}")
        ]).Handle(_grainFactory, _causation, _principal);

    [Fact] void should_append_all_events() => _appendedEvents.Length.ShouldEqual(3);
    [Fact] void should_default_empty_source_type() => _appendedEvents[0].EventSourceType.ShouldEqual(EventSourceType.Default);
    [Fact] void should_default_empty_stream_type() => _appendedEvents[0].eventStreamType.ShouldEqual(EventStreamType.All);
    [Fact] void should_default_empty_stream_id() => _appendedEvents[0].eventStreamId.ShouldEqual((EventStreamId)EventStreamId.Default);
    [Fact] void should_preserve_explicit_source_type() => _appendedEvents[1].EventSourceType.ShouldEqual(new EventSourceType("Account"));
    [Fact] void should_preserve_explicit_stream_type() => _appendedEvents[1].eventStreamType.ShouldEqual(new EventStreamType("Payments"));
    [Fact] void should_preserve_explicit_stream_id() => _appendedEvents[1].eventStreamId.ShouldEqual(new EventStreamId("September"));
    [Fact] void should_default_null_source_type() => _appendedEvents[2].EventSourceType.ShouldEqual(EventSourceType.Default);
    [Fact] void should_default_null_stream_type() => _appendedEvents[2].eventStreamType.ShouldEqual(EventStreamType.All);
    [Fact] void should_default_null_stream_id() => _appendedEvents[2].eventStreamId.ShouldEqual((EventStreamId)EventStreamId.Default);
}
