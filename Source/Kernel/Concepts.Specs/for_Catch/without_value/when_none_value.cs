// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using OneOf.Types;

namespace Cratis.Chronicle.Concepts.for_Catch.without_value;

public class when_none_value : Specification
{
    static Catch result;


    void Because() => result = Catch.Success();

    [Fact] void should_be_success() => result.IsSuccess.ShouldBeTrue();
    [Fact] void should_not_get_error() => result.TryGetError(out _).ShouldBeFalse();
    [Fact] void should_have_the_none_value() => result.Match<object>(_ => _, error => error).ShouldEqual(default(None));
}
