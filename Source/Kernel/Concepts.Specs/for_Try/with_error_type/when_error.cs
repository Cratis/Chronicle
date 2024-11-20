// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Auditing;
using Cratis.Chronicle.Concepts.Identities;
using Cratis.Chronicle.Storage;

namespace Cratis.Chronicle.Concepts.for_Try.with_error_type;

public class when_error : Specification
{
    static Try<int, TheErrorType> result;
    static Exception error;

    void Establish() => error = new Exception();

    void Because() => result = Try<int, TheErrorType>.Failed(error);

    [Fact] void should_not_be_success() => result.IsSuccess.ShouldBeFalse();
    [Fact] void should_not_have_result() => result.TryGetResult(out _).ShouldBeFalse();
    [Fact] void should_have_the_error() => result.Match(_ => _, errorType => errorType.Value).ShouldEqual(error);
}
