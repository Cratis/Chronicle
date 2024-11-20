// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage;

namespace Cratis.Chronicle.Concepts.for_Try.with_error_type_and_value;

public class when_error_type : Specification
{
    static Try<int, TheErrorType> result;
    static TheErrorType errorType;

    void Establish() => errorType = TheErrorType.SomeOtherType;

    void Because() => result = Try<int, TheErrorType>.Failed(errorType);

    [Fact] void should_not_be_success() => result.IsSuccess.ShouldBeFalse();
    [Fact] void should_not_have_result() => result.TryGetResult(out _).ShouldBeFalse();
    [Fact] void should_have_the_error() => result.Match<object>(_ => _, errorType => errorType).ShouldEqual(errorType);
}
