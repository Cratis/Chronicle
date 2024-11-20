// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage;

namespace Cratis.Chronicle.Concepts.for_Try.with_safe_error_type;

public class when_error_type : Specification
{
    static SafeTry<int, TheErrorType> result;
    static TheErrorType errorType;

    void Establish() => errorType = TheErrorType.SomeOtherType;

    void Because() => result = SafeTry<int, TheErrorType>.Failed(errorType);

    [Fact] void should_not_be_success() => result.IsSuccess.ShouldBeFalse();
    [Fact] void should_not_have_result() => result.TryGetResult(out _).ShouldBeFalse();
    [Fact] void should_have_the_error() => result.Match(_ => _, errorType => errorType.Value).ShouldEqual(errorType);
}
