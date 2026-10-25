// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Chronicle.Events.for_EventObservationStateJsonConverter;

public class when_reading_states : Specification
{
    readonly JsonSerializerOptions _options = new() { Converters = { new EventObservationStateJsonConverter() } };

    [Fact] void should_read_none() => Read("\"None\"").ShouldEqual(EventObservationState.None);
    [Fact] void should_read_initial() => Read("\"initial\"").ShouldEqual(EventObservationState.Initial);
    [Fact] void should_read_replay() => Read("\"REPLAY\"").ShouldEqual(EventObservationState.Replay);
    [Fact] void should_read_combined_flags() => Read("\"Initial, Replay\"").ShouldEqual(EventObservationState.Initial | EventObservationState.Replay);
    [Fact] void should_read_legacy_none() => Read("0").ShouldEqual(EventObservationState.None);
    [Fact] void should_read_legacy_initial() => Read("1").ShouldEqual(EventObservationState.Initial);
    [Fact] void should_read_legacy_replay() => Read("2").ShouldEqual(EventObservationState.Replay);
    [Fact] void should_read_legacy_combined_flags() => Read("3").ShouldEqual(EventObservationState.Initial | EventObservationState.Replay);
    [Fact] void should_keep_numeric_writes() => JsonSerializer.Serialize(EventObservationState.Replay, _options).ShouldEqual("2");
    [Fact] void should_write_combined_flags() => JsonSerializer.Serialize(EventObservationState.Initial | EventObservationState.Replay, _options).ShouldEqual("3");
    [Fact] void should_reject_unknown_names() => Catch.Exception(() => Read("\"Unknown\"")).ShouldBeOfExactType<JsonException>();
    [Fact] void should_reject_unknown_flags() => Catch.Exception(() => Read("4")).ShouldBeOfExactType<JsonException>();
    [Fact] void should_reject_numeric_strings() => Catch.Exception(() => Read("\"2\"")).ShouldBeOfExactType<JsonException>();

    EventObservationState Read(string json) => JsonSerializer.Deserialize<EventObservationState>(json, _options);
}
