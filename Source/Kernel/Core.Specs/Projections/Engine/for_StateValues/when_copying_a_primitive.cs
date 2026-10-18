// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Engine.for_StateValues;

public class when_copying_a_primitive : Specification
{
    object? _result;

    void Because() => _result = StateValues.Copy("value");

    [Fact] void should_return_the_value() => _result.ShouldEqual("value");
}
