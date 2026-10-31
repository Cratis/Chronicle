// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Events.for_TypedEventSourceId.when_round_tripping_under_norwegian_culture;

public class with_a_date_time_offset : given.a_norwegian_culture
{
    static readonly DateTimeOffset _input = new DateTimeOffset(2026, 10, 23, 14, 15, 16, TimeSpan.FromHours(2)).AddTicks(1234567);
    EventSourceId<DateTimeOffset> _result;

    void Because() => WithNorwegianCulture(() => _result = EventSourceId<DateTimeOffset>.From(new EventSourceId<DateTimeOffset>(_input)));

    [Fact] void should_preserve_the_typed_value() => _result.TypedValue.ShouldEqual(_input);
    [Fact] void should_preserve_the_offset() => _result.TypedValue.Offset.ShouldEqual(TimeSpan.FromHours(2));
}
