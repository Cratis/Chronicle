// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.for_SafeOption;

public class when_error : Specification
{
    static SafeOption<int> result;
    static Exception error;

    void Establish() => error = new Exception();
    void Because() => result = SafeOption<int>.Error(error);

    [Fact] void should_not_have_value() => result.HasValue.ShouldBeFalse();
    [Fact] void should_not_have_result() => result.TryGetValue(out _).ShouldBeFalse();
    [Fact] void should_have_error() => result.TryGetError(out _).ShouldBeTrue();
    [Fact] void should_have_the_error() => result.Match(value => value, theTry => theTry.Value).ShouldEqual(error);
}

