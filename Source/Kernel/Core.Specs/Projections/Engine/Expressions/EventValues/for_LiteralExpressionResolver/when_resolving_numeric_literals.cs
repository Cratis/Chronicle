// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Engine.Expressions.EventValues.for_LiteralExpressionResolver;

public class when_resolving_numeric_literals : given.an_appended_event
{
    LiteralExpressionResolver _resolver;
    object _integer;
    object _fraction;
    object _negative;

    void Establish() => _resolver = new();

    void Because()
    {
        _integer = _resolver.Resolve("1")(@event);
        _fraction = _resolver.Resolve("4.5")(@event);
        _negative = _resolver.Resolve("-2")(@event);
    }

    [Fact] void should_recognize_invariant_numbers() => _resolver.CanResolve("4.5").ShouldBeTrue();
    [Fact] void should_return_an_integer_literal_as_a_number() => _integer.ShouldEqual(1L);
    [Fact] void should_return_a_fractional_literal_as_a_number() => _fraction.ShouldEqual(4.5m);
    [Fact] void should_return_a_negative_literal_as_a_number() => _negative.ShouldEqual(-2L);
}
