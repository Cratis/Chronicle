// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Events.for_TypedEventSourceId.when_converting_to_an_event_source_id;

public class with_a_date_time_under_norwegian_culture : given.a_norwegian_culture
{
    EventSourceId _result;

    void Because() => _result = new EventSourceId<DateTime>(new DateTime(2026, 10, 23, 14, 15, 16, DateTimeKind.Utc).AddTicks(1234567));

    [Fact] void should_use_the_invariant_round_trip_format() => _result.Value.ShouldEqual("2026-10-23T14:15:16.1234567Z");
}
