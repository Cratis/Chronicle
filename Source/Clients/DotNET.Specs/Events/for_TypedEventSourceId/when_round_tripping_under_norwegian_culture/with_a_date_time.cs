// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Events.for_TypedEventSourceId.when_round_tripping_under_norwegian_culture;

public class with_a_date_time : given.a_norwegian_culture
{
    static readonly DateTime _input = new DateTime(2026, 10, 23, 14, 15, 16, DateTimeKind.Utc).AddTicks(1234567);
    EventSourceId<DateTime> _result;

    void Because() => _result = EventSourceId<DateTime>.From(new EventSourceId<DateTime>(_input));

    [Fact] void should_preserve_the_typed_value() => _result.TypedValue.ShouldEqual(_input);
    [Fact] void should_preserve_the_kind() => _result.TypedValue.Kind.ShouldEqual(DateTimeKind.Utc);
}
