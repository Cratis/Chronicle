// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Events.for_TypedEventSourceId.when_round_tripping_under_norwegian_culture;

public class with_a_concept_wrapping_a_decimal : given.a_norwegian_culture
{
    EventSourceId _untyped;
    EventSourceId<DecimalConcept> _result;

    void Because()
    {
        _untyped = new EventSourceId<DecimalConcept>(new DecimalConcept(123.45m));
        _result = EventSourceId<DecimalConcept>.From(_untyped);
    }

    [Fact] void should_use_the_invariant_decimal_separator() => _untyped.Value.ShouldEqual("123.45");
    [Fact] void should_preserve_the_typed_value() => _result.TypedValue.ShouldEqual(new DecimalConcept(123.45m));
}
