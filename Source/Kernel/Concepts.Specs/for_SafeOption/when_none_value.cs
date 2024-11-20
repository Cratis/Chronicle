// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using OneOf.Types;

namespace Cratis.Chronicle.Concepts.for_SafeOption;

public class when_none_value : Specification
{
    static SafeOption<int> result;


    void Because() => result = SafeOption<int>.None();

    [Fact] void should_not_have_value() => result.HasValue.ShouldBeFalse();
    [Fact] void should_not_have_result() => result.TryGetValue(out _).ShouldBeFalse();
    [Fact] void should_not_have_error() => result.TryGetError(out _).ShouldBeFalse();
    [Fact] void should_have_the_none_value() => result.Match(value => value, theTry => theTry.Value).ShouldEqual(default(None));
}

