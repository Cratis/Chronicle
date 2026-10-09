// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Events.for_TypedEventSourceId.when_round_tripping_under_norwegian_culture;

public class with_a_double : given.a_norwegian_culture
{
    EventSourceId _untyped;
    EventSourceId<double> _result;

    void Because()
    {
        _untyped = new EventSourceId<double>(123.45);
        _result = _untyped;
    }

    [Fact] void should_use_the_invariant_decimal_separator() => _untyped.Value.ShouldEqual("123.45");
    [Fact] void should_preserve_the_typed_value() => _result.TypedValue.ShouldEqual(123.45);
}
