// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using OneOf.Types;

namespace Cratis.Chronicle.Concepts.for_Try.with_value;

public class when_not_error : Specification
{
    static Try<TheErrorType> result;


    void Because() => result = Try<TheErrorType>.Success();

    [Fact] void should_be_success() => result.IsSuccess.ShouldBeTrue();
    [Fact] void should_not_have_error() => result.TryGetError(out _).ShouldBeFalse();
    [Fact] void should_have_the_value() => result.Match<object>(_ => _, error => error).ShouldEqual(default(None));
}
