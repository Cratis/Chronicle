// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage;

namespace Cratis.Chronicle.Concepts.for_Try.with_value;

public class when_value : Specification
{
    static Try<int> result;
    static int value;

    void Establish() => value = 42;

    void Because() => result = Try<int>.Success(value);

    [Fact] void should_be_success() => result.IsSuccess.ShouldBeTrue();
    [Fact] void should_have_result() => result.TryGetResult(out _).ShouldBeTrue();
    [Fact] void should_have_the_value() => result.Match<object>(_ => _, error => error).ShouldEqual(value);
}
