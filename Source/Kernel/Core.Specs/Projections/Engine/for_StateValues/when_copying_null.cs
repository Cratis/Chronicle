// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Engine.for_StateValues;

public class when_copying_null : Specification
{
    object? _result;

    void Because() => _result = StateValues.Copy(null);

    [Fact] void should_return_null() => _result.ShouldBeNull();
}
