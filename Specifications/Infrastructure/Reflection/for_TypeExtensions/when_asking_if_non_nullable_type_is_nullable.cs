// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Reflection.for_TypeExtensions;

public class when_asking_if_non_nullable_type_is_nullable : Specification
{
    static bool result;

    void Because() => result = typeof(int).IsNullable();

    [Fact] void should_return_false() => result.ShouldBeFalse();
}
