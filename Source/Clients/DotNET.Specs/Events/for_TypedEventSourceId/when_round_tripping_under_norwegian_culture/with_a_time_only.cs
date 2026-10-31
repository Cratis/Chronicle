// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Events.for_TypedEventSourceId.when_round_tripping_under_norwegian_culture;

public class with_a_time_only : given.a_norwegian_culture
{
    static readonly TimeOnly _input = new TimeOnly(14, 15, 16).Add(TimeSpan.FromTicks(1234567));
    EventSourceId _untyped;
    EventSourceId<TimeOnly> _result;

    void Because() => WithNorwegianCulture(() =>
    {
        _untyped = new EventSourceId<TimeOnly>(_input);
        _result = EventSourceId<TimeOnly>.From(_untyped);
    });

    [Fact] void should_use_the_invariant_round_trip_format() => _untyped.Value.ShouldEqual("14:15:16.1234567");
    [Fact] void should_preserve_the_typed_value() => _result.TypedValue.ShouldEqual(_input);
}
