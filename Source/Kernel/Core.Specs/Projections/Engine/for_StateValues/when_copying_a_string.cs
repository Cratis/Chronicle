// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Engine.for_StateValues;

public class when_copying_a_string : Specification
{
    const string Original = "value";
    object? _result;

    void Because() => _result = StateValues.Copy(Original);

    [Fact] void should_return_the_same_string() => _result.ShouldBeSame(Original);
}
