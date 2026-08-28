// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Patterns.for_LossyCountingSketch.when_creating;

/// <summary>
/// The error parameter is what bounds the sketch's memory. A value outside zero to one is not a slow sketch, it is
/// one whose bucket width is meaningless - so it fails at construction rather than mining nonsense quietly.
/// </summary>
public class with_an_error_parameter_outside_the_range : Specification
{
    static LossyCountingSketch _created;

    static Exception CreatingWith(double error) => Catch.Exception(() => _created = new LossyCountingSketch(error, 1d));

    [Fact] void should_reject_zero() => CreatingWith(0d).ShouldNotBeNull();
    [Fact] void should_reject_a_negative_value() => CreatingWith(-0.1d).ShouldNotBeNull();
    [Fact] void should_reject_one() => CreatingWith(1d).ShouldNotBeNull();
    [Fact] void should_accept_a_value_between_zero_and_one() => CreatingWith(0.001d).ShouldBeNull();
}
