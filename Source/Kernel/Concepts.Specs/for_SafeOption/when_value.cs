// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.for_SafeOption;

public class when_value : Specification
{
    static SafeOption<int> result;
    static int value;

    void Establish() => value = 42;

    void Because() => result = value;

    [Fact] void should_have_value() => result.HasValue.ShouldBeTrue();
    [Fact] void should_have_result() => result.TryGetValue(out _).ShouldBeTrue();
    [Fact] void should_not_have_error() => result.TryGetError(out _).ShouldBeFalse();
    [Fact] void should_have_the_value() => result.Match<object>(value => value, none => none).ShouldEqual(value);
}

