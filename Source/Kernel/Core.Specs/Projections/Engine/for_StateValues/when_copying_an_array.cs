// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Engine.for_StateValues;

public class when_copying_an_array : Specification
{
    object[] _original;
    object _result;

    void Establish() => _original = ["one", "two"];

    void Because() => _result = StateValues.Copy(_original);

    [Fact] void should_return_a_list() => _result.ShouldBeOfExactType<List<object>>();
    [Fact] void should_keep_the_items() => ((List<object>)_result).ShouldContainOnly(_original);
}
