// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Events.for_TypedEventSourceId.when_round_tripping_under_norwegian_culture;

public class with_a_date_only : given.a_norwegian_culture
{
    EventSourceId _untyped;
    EventSourceId<DateOnly> _result;

    void Because()
    {
        _untyped = new EventSourceId<DateOnly>(new DateOnly(2026, 10, 23));
        _result = EventSourceId<DateOnly>.From(_untyped);
    }

    [Fact] void should_use_the_invariant_round_trip_format() => _untyped.Value.ShouldEqual("2026-10-23");
    [Fact] void should_preserve_the_typed_value() => _result.TypedValue.ShouldEqual(new DateOnly(2026, 10, 23));
}
