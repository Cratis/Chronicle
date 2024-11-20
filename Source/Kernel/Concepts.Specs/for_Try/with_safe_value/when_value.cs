// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.for_Try.with_safe_value;

public class when_value : Specification
{
    static SafeTry<int> result;
    static int value;

    void Establish() => value = 42;

    void Because() => result = SafeTry<int>.Success(value);

    [Fact] void should_be_success() => result.IsSuccess.ShouldBeTrue();
    [Fact] void should_have_result() => result.TryGetResult(out _).ShouldBeTrue();
    [Fact] void should_have_the_value() => result.Match<object>(_ => _, errorType => errorType).ShouldEqual(value);
}
