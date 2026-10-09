// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Events.for_TypedEventSourceId.when_round_tripping_under_norwegian_culture;

public class with_a_decimal : given.a_norwegian_culture
{
    EventSourceId<decimal> _result;

    void Because() => _result = EventSourceId<decimal>.From(new EventSourceId<decimal>(123.45m));

    [Fact] void should_preserve_the_typed_value() => _result.TypedValue.ShouldEqual(123.45m);
}
