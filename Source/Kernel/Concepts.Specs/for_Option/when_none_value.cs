// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using OneOf.Types;

namespace Cratis.Chronicle.Concepts.for_Option;

public class when_none_value : Specification
{
    static Option<int> result;


    void Because() => result = Option<int>.None();

    [Fact] void should_not_have_value() => result.HasValue.ShouldBeFalse();
    [Fact] void should_not_have_result() => result.TryGetValue(out _).ShouldBeFalse();
    [Fact] void should_have_the_none_value() => result.Match<object>(value => value, none => none).ShouldEqual(default(None));
}

