// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Monads.for_Catch.with_error_type;

public class when_error : Specification
{
    static Catch<int, TheErrorType> result;
    static Exception error;

    void Establish() => error = new Exception();

    void Because() => result = Catch<int, TheErrorType>.Failed(error);

    [Fact] void should_not_be_success() => result.IsSuccess.ShouldBeFalse();
    [Fact] void should_not_have_result() => result.TryGetResult(out _).ShouldBeFalse();
    [Fact] void should_have_the_error() => result.Match<object>(_ => _, errorType => errorType, error => error).ShouldEqual(error);
}
