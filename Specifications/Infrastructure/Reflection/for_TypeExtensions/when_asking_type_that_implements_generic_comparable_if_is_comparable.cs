// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Reflection.for_TypeExtensions;

public class when_asking_type_that_implements_generic_comparable_if_is_comparable : Specification
{
    bool result;

    void Because() => result = Mock.Of<IComparable<int>>().GetType().IsComparable();

    [Fact] void should_be_considered_comparable() => result.ShouldBeTrue();
}
