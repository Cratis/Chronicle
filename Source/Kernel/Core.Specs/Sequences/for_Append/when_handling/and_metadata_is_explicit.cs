// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Sequences.for_Append.when_handling;

public class and_metadata_is_explicit : Sequences.given.an_append_endpoint
{
    async Task Because() => await new Append("store", "namespace", "event-log", "source", new EventSourceType("Account"), new EventStreamType("Payments"), new EventStreamId("September"), new EventType("event", 1, false), "{}")
        .Handle(_grainFactory, _causation, _principal);

    [Fact] void should_append_one_event() => _appendedEvents.Length.ShouldEqual(1);
    [Fact] void should_use_the_expected_source_type() => _appendedEvents[0].EventSourceType.ShouldEqual(new EventSourceType("Account"));
    [Fact] void should_use_the_expected_stream_type() => _appendedEvents[0].eventStreamType.ShouldEqual(new EventStreamType("Payments"));
    [Fact] void should_use_the_expected_stream_id() => _appendedEvents[0].eventStreamId.ShouldEqual(new EventStreamId("September"));
}
