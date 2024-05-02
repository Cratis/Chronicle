// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Reflection.for_TypeExtensions;

public class when_asking_a_type_with_a_default_and_a_non_default_constructor_if_it_has_a_default_constructor : Specification
{
    bool result;

    void Because() => result = typeof(TypeWithDefaultAndNonDefaultConstructor).HasDefaultConstructor();

    [Fact] void should_return_true() => result.ShouldBeTrue();
}
