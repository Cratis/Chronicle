// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.for_Result.with_error_type_and_value;

public class when_error_type : Specification
{
    static Result<int, TheErrorType> result;
    static TheErrorType errorType;

    void Establish() => errorType = TheErrorType.SomeOtherType;

    void Because() => result = Result<int, TheErrorType>.Failed(errorType);

    [Fact] void should_not_be_success() => result.IsSuccess.ShouldBeFalse();
    [Fact] void should_not_have_result() => result.TryGetResult(out _).ShouldBeFalse();
    [Fact] void should_have_the_error() => result.Match<object>(_ => _, errorType => errorType).ShouldEqual(errorType);
}
